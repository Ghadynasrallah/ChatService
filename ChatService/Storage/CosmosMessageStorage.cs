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
    
    public async Task<ListMessagesStorageResponseDto> EnumerateMessagesFromAGivenConversation(
        string conversationId,
        string? continuationToken = null,
        int? limit = null,
        long? lastSeenMessageTime = null)
    {
        List<Message> messagesResult = new List<Message>();
        var queryOptions = new QueryRequestOptions
        {
            PartitionKey = new PartitionKey(conversationId),
            ConsistencyLevel = ConsistencyLevel.Session,
            MaxItemCount = limit ?? -1
        };
        
        var queryText = "SELECT * FROM c ORDER BY c.unixTime DESC";
        if (lastSeenMessageTime != null)
        { 
            queryText = $"SELECT * FROM c WHERE c.unixTime > {lastSeenMessageTime.ToString()} ORDER BY c.unixTime DESC";
        }
        var iterator = container.GetItemQueryIterator<MessageEntity>(requestOptions: queryOptions, queryText: queryText, continuationToken: continuationToken);
        FeedResponse<MessageEntity>? response = null;
        if (iterator.HasMoreResults)
        {
            response = await iterator.ReadNextAsync();
            foreach (var messageEntity in response)
            {
                messagesResult.Add(ToMessage(messageEntity));
            }
        }
        return new ListMessagesStorageResponseDto(messagesResult, response?.ContinuationToken);
    }
    
    public async Task PostMessageToConversation(Message message)
    {
        await container.UpsertItemAsync(ToMessageEntity(message));
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
    
    private static MessageEntity ToMessageEntity(Message message)
    {
        return new MessageEntity(message.ConversationId, message.MessageId, message.Text, message.SenderUsername,
            message.UnixTime);
    }
}