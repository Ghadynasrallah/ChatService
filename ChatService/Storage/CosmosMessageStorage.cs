using System.Net;
using ChatService.Dtos;
using ChatService.Storage.Entities;
using Microsoft.Azure.Cosmos;

namespace ChatService.Storage;

public class CosmosMessageStorage : IMessageStorage
{
    private readonly CosmosClient _cosmosClient;

    public CosmosMessageStorage(CosmosClient cosmosClient)
    {
        _cosmosClient = cosmosClient;
    }

    private Container container => _cosmosClient.GetDatabase("ChatService").GetContainer("Messages");
    
    public async Task<EnumerateMessagesInAConversationResponseDto?> EnumerateMessagesFromAGivenConversation(string conversationId,
        string? continuationToken = null, 
        int? limit = null,
        long? lastSeenMessageTime=null)
    {
        try
        {
            List<Message> messagesResult = new List<Message>();
            var queryOptions = new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(conversationId),
                ConsistencyLevel = ConsistencyLevel.Session,
                MaxItemCount = limit ?? -1
            };
            
            var queryText = "SELECT * FROM c ORDER BY c.unixTime ASC";
            if (lastSeenMessageTime != null)
            { 
                queryText = $"SELECT * FROM c WHERE c.unixTime > {lastSeenMessageTime.ToString()} ORDER BY c.unixTime ASC";
            }
            var iterator = container.GetItemQueryIterator<MessageEntity>(requestOptions: queryOptions, queryText: queryText, continuationToken: continuationToken);
            FeedResponse<MessageEntity> response = null;
            while (iterator.HasMoreResults)
            {
                response = await iterator.ReadNextAsync();
                foreach (var messageEntity in response)
                {
                    messagesResult.Add(ToMessage(messageEntity));
                }
                response = await iterator.ReadNextAsync();
            }
            return new EnumerateMessagesInAConversationResponseDto(messagesResult, response.ContinuationToken);
        }
        catch (CosmosException e)
        {
            if (e.StatusCode == HttpStatusCode.NotFound)
                return null;
            throw;
        }
    }
    
    public async Task PostMessageToConversation(Message message)
    {
        if (String.IsNullOrWhiteSpace(message.conversationId) ||
            String.IsNullOrWhiteSpace(message.text) ||
            String.IsNullOrWhiteSpace(message.senderUsername) ||
            String.IsNullOrWhiteSpace(message.messageId))
        {
            throw new ArgumentException($"Invalid message {message}", nameof(message));
        }

        await container.UpsertItemAsync(ToMessageEntityForFirstConversation(message));
        await container.UpsertItemAsync(ToMessageEntityForSecondConversation(message));
    }

    public async Task<Message?> GetMessage(string conversationId, string messageId)
    {
        try
        {
            var messageEntity = await container.ReadItemAsync<MessageEntity>(
                id: messageId,
                partitionKey: new PartitionKey(conversationId),
                new ItemRequestOptions
                {
                    ConsistencyLevel = ConsistencyLevel.Session
                });
            return ToMessage(messageEntity);
        }
        catch (CosmosException e)
        {
            if (e.StatusCode == HttpStatusCode.NotFound)
                return null;
            throw;
        }
    }
    
    public async Task<bool> DeleteMessage(string conversationId, string messageId)
    {
        try
        {
            await container.DeleteItemAsync<MessageEntity>(
                id: messageId,
                partitionKey: new PartitionKey(conversationId),
                new ItemRequestOptions
                {
                    ConsistencyLevel = ConsistencyLevel.Session
                });
            await container.DeleteItemAsync<MessageEntity>(
                id: messageId,
                partitionKey: new PartitionKey(FlipUsernamesInConversationId(conversationId)),
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

    private static Message ToMessage(MessageEntity messageEntity)
    {
        return new Message(messageEntity.id, messageEntity.text, messageEntity.senderUsername,
            messageEntity.partitionKey, messageEntity.unixTime);
    }

    private static MessageEntity ToMessageEntityForFirstConversation(Message message)
    {
        return new MessageEntity(message.conversationId, message.messageId, message.text, message.senderUsername,
            message.unixTime);
    }
    
    private static MessageEntity ToMessageEntityForSecondConversation(Message message)
    {
        return new MessageEntity(FlipUsernamesInConversationId(message.conversationId), message.messageId, message.text, message.senderUsername,
            message.unixTime);
    }

    private static string FlipUsernamesInConversationId(string conversationId)
    {
        string[] userIds = conversationId.Split("_");
        return $"{userIds[1]}_{userIds[0]}";
    }
}