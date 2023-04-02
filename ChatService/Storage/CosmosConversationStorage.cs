using System.Net;
using ChatService.Dtos;
using ChatService.Storage.Entities;
using Microsoft.Azure.Cosmos;

namespace ChatService.Storage;

public class CosmosConversationStorage : IConversationStorage
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

    public async Task<Conversation?> GetConversation(string conversationId)
    {
        if (String.IsNullOrWhiteSpace(conversationId))
        {
            throw new ArgumentException("Invalid conversation ID: ID does not contain any text");
        }

        try
        {
            string[] parts = conversationId.Split("_");
            string userId1 = parts[0];
            var conversationEntity = await container.ReadItemAsync<ConversationEntity>(
                id: conversationId,
                partitionKey: new PartitionKey(userId1),
                new ItemRequestOptions
                {
                    ConsistencyLevel = ConsistencyLevel.Session
                }
            );
            return ToConversation(conversationEntity);
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

    public async Task<bool> DeleteConversation(string conversationId)
    {
        if (String.IsNullOrWhiteSpace(conversationId))
        {
            throw new ArgumentException("Invalid conversation ID: ID does not contain any text");
        }

        try
        {
            string[] parts = conversationId.Split("_");
            string userId1 = parts[0];
            string userId2 = parts[1];
            await container.DeleteItemAsync<ConversationEntity>(
                id: conversationId,
                partitionKey: new PartitionKey(userId1),
                new ItemRequestOptions
                {
                    ConsistencyLevel = ConsistencyLevel.Session
                });
            await container.DeleteItemAsync<ConversationEntity>(
                id: $"{userId2}_{userId1}",
                partitionKey: new PartitionKey(userId2),
                new ItemRequestOptions
                {
                    ConsistencyLevel = ConsistencyLevel.Session
                });
            return true;
        }
        catch (CosmosException e)
        {
            if (e.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }

            throw;
        }
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
        return new ConversationEntity(conversation.userId1, $"{conversation.userId2}_{conversation.userId1}",
            conversation.userId2, conversation.userId1, conversation.lastModifiedUnixTime);
    } 
}