using System.Net;
using ChatService.Dtos;
using ChatService.Storage.Entities;
using Microsoft.Azure.Cosmos;

namespace ChatService.Storage;

public class CosmosConversationStorage
{
    private readonly CosmosClient _cosmosClient;
    
    public CosmosConversationStorage(CosmosClient cosmosClient)
    {
        _cosmosClient = cosmosClient;
    }

    private Container container => _cosmosClient.GetDatabase("ChatService").GetContainer("Conversations");

    public async Task<List<Conversation>?> EnumerateConversationsForAGivenUser(string userId)
    {
        try
        {
            List<Conversation> conversationsResult = new List<Conversation>();
            var queryOptions = new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(userId),
                ConsistencyLevel = ConsistencyLevel.Session
            };
            var iterator = container.GetItemQueryIterator<ConversationEntity>(requestOptions: queryOptions);
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                foreach (var conversationEntity in response)
                {
                    conversationsResult.Add(ToConversation(conversationEntity));
                }
            }

            return conversationsResult;
        }
        catch (CosmosException e)
        {
            if (e.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            throw;
        }
    }

    public async Task PostConversation(Conversation conversation)
    {
        if (String.IsNullOrWhiteSpace(conversation.conversationId) ||
            String.IsNullOrWhiteSpace(conversation.userId1) ||
            String.IsNullOrWhiteSpace(conversation.userId2))
        {
            throw new ArgumentException($"Invalid conversation {conversation}", nameof(conversation));
        }

        await container.UpsertItemAsync(ToConversationEntityForUser1(conversation));
        await container.UpsertItemAsync(ToConversationEntityForUser2(conversation));
    }

    private static Conversation ToConversation(ConversationEntity conversationEntity)
    {
        return new Conversation(conversationEntity.id, conversationEntity.userId1, conversationEntity.userId2,
            conversationEntity.lastModifiedUnixTime);
    }

    private static ConversationEntity ToConversationEntityForUser1(Conversation conversation)
    {
        return new ConversationEntity(conversation.userId1, $"{conversation.userId1}_{conversation.userId2}",
            conversation.userId1, conversation.userId2, conversation.lastModifiedUnixTime);
    } 
    
    private static ConversationEntity ToConversationEntityForUser2(Conversation conversation)
    {
        return new ConversationEntity(conversation.userId1, $"{conversation.userId1}_{conversation.userId2}",
            conversation.userId1, conversation.userId2, conversation.lastModifiedUnixTime);
    } 
}