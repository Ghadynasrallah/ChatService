using System.Net;
using ChatService.Dtos;
using ChatService.Storage.Entities;
using Microsoft.Azure.Cosmos;

namespace ChatService.Storage;

public class CosmosMessageStorage
{
    private readonly CosmosClient _cosmosClient;

    public CosmosMessageStorage(CosmosClient cosmosClient)
    {
        _cosmosClient = cosmosClient;
    }

    private Container container => _cosmosClient.GetDatabase("ChatService").GetContainer("Messages");
    
    public async Task<List<Message>?> EnumerateMessagesForAGivenConversation(string conversationId)
    {
        try
        {
            List<Message> messagesResult = new List<Message>();
            var queryOptions = new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(conversationId),
                ConsistencyLevel = ConsistencyLevel.Session
            };
            var iterator = container.GetItemQueryIterator<MessageEntity>(requestOptions: queryOptions);
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                foreach (var messageEntity in response)
                {
                    messagesResult.Add(ToMessage(messageEntity));
                }
            }

            return messagesResult;
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

    public async Task PostMessageToConversation(Message message)
    {
        if (String.IsNullOrWhiteSpace(message.conversationId) ||
            String.IsNullOrWhiteSpace(message.text) ||
            String.IsNullOrWhiteSpace(message.senderUsername) ||
            String.IsNullOrWhiteSpace(message.messageId))
        {
            throw new ArgumentException($"Invalid message {message}", nameof(message));
        }

        await container.UpsertItemAsync(message);
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
            {
                return null;
            }

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
        return new MessageEntity(message.conversationId, message.messageId, message.text, message.senderUsername,
            message.unixTime);
    }
}