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
    
    public async Task<EnumerateConversationsStorageResponseDto?> EnumerateConversationsForAGivenUser(
        string conversationId,
        string? continuationToken = null,
        int? limit = null,
        long? lastSeenMessageTime = null)
    {
        try
        {
            List<Conversation> conversationsResult = new List<Conversation>();
            var queryOptions = new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(conversationId),
                ConsistencyLevel = ConsistencyLevel.Session,
                MaxItemCount = limit ?? -1
            };
            
            var queryText = "SELECT * FROM c ORDER BY c.lastModifiedUnixTime ASC";
            if (lastSeenMessageTime != null)
            { 
                queryText = $"SELECT * FROM c WHERE c.lastModifiedUnixTime > {lastSeenMessageTime.ToString()} ORDER BY c.lastModifiedUnixTime ASC";
            }
            var iterator = container.GetItemQueryIterator<ConversationEntity>(requestOptions: queryOptions, queryText: queryText, continuationToken: continuationToken);
            FeedResponse<ConversationEntity>? response = null;
            while (iterator.HasMoreResults)
            {
                response = await iterator.ReadNextAsync();
                foreach (var conversationEntity in response)
                {
                    conversationsResult.Add(ToConversation(conversationEntity));
                }
            }
            if (response?.ContinuationToken != null)
                return new EnumerateConversationsStorageResponseDto(conversationsResult, response.ContinuationToken);
            return new EnumerateConversationsStorageResponseDto(conversationsResult, null);
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

    public async Task<string> PostConversation(Conversation conversation)
    {
        if (String.IsNullOrWhiteSpace(conversation.userId1) ||
            String.IsNullOrWhiteSpace(conversation.userId2))
        {
            throw new ArgumentException($"Invalid conversation {conversation}", nameof(conversation));
        }
        await container.UpsertItemAsync(ToConversationEntity(conversation, conversation.userId1));
        await container.UpsertItemAsync(ToConversationEntity(conversation, conversation.userId2));
        return GetConversationId(conversation.userId1, conversation.userId2);
    }

    public async Task<Conversation?> GetConversation(string userId1,string userId2)
    {
        if (String.IsNullOrWhiteSpace(userId1) ||
            String.IsNullOrWhiteSpace(userId2))
        {
            throw new ArgumentException("Invalid arguments");
        }

        try
        {
            string conversationId = GetConversationId(userId1, userId2);
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
                return null;
            throw;
        }
    }

    public async Task<bool> DeleteConversation(string userId1, string userId2)
    {
        if (String.IsNullOrWhiteSpace(userId1) ||
            String.IsNullOrWhiteSpace(userId2))
        {
            throw new ArgumentException("Invalid arguments");
        }

        try
        {
            string conversationId = GetConversationId(userId1, userId2);
            await container.DeleteItemAsync<ConversationEntity>(
                id: conversationId,
                partitionKey: new PartitionKey(userId1),
                new ItemRequestOptions
                {
                    ConsistencyLevel = ConsistencyLevel.Session
                });
            await container.DeleteItemAsync<ConversationEntity>(
                id: conversationId,
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
                return false;
            throw;
        }
    }

    private static Conversation ToConversation(ConversationEntity conversationEntity)
    {
        return new Conversation(conversationEntity.userId1, conversationEntity.userId2,
            conversationEntity.lastModifiedUnixTime);
    }
    
    private static string GetConversationId(string userId1, string userId2)
    {
        return string.CompareOrdinal(userId1, userId2) < 0 ? $"{userId1}_{userId2}" : $"{userId2}_{userId1}";
    }
    
    private static ConversationEntity ToConversationEntity(Conversation conversation, string partitionKey)
    {
        string conversationId = GetConversationId(conversation.userId1, conversation.userId2);
        return new ConversationEntity(
            partitionKey: partitionKey,
            id:conversationId,
            userId1: conversation.userId1,
            userId2: conversation.userId2,
            lastModifiedUnixTime: conversation.lastModifiedUnixTime);
    }
}