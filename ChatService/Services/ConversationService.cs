using System.Web;
using ChatService.Dtos;
using ChatService.Exceptions;
using ChatService.Storage;

namespace ChatService.Services;

public class ConversationService
{
    private readonly IConversationStorage _conversationStorage;
    private readonly IMessageStorage _messageStorage;
    private readonly IProfileStorage _profileStorage;

    public ConversationService(IConversationStorage conversationStorage, IMessageStorage messageStorage, IProfileStorage profileStorage)
    {
        _conversationStorage = conversationStorage;
        _messageStorage = messageStorage;
        _profileStorage = profileStorage;
    }

    public async Task<Message> SendMessageToConversation(string conversationId, SendMessageRequest sendMessageRequest)
    {
        if (String.IsNullOrWhiteSpace(conversationId))
        {
            throw new ArgumentException($"Invalid conversation ID {conversationId}");
        }
        if (String.IsNullOrWhiteSpace(sendMessageRequest.text) ||
            String.IsNullOrWhiteSpace(sendMessageRequest.senderUsername) ||
            String.IsNullOrWhiteSpace(sendMessageRequest.messageId))
        {
            throw new ArgumentException($"Invalid message {sendMessageRequest}", nameof(sendMessageRequest));
        }
        
        var message = new Message(
            sendMessageRequest.messageId,
            sendMessageRequest.text,
            sendMessageRequest.senderUsername,
            conversationId,
            DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        );
        
        await _messageStorage.PostMessageToConversation(message);
        return message;
    }

    public async Task<EnumerateMessagesStorageResponseDto?> EnumerateMessagesInAConversation(
        string conversationId,
        string? continuationToken = null,
        int? limit = null,
        long? lastSeenMessageTime = null)
    {
        if (String.IsNullOrWhiteSpace(conversationId))
        {
            throw new ArgumentException($"Invalid conversation ID {conversationId}");
        }

        await _conversationStorage.GetConversation(conversationId);
        var response = await _messageStorage.EnumerateMessagesFromAGivenConversation(conversationId,
            continuationToken, limit, lastSeenMessageTime);
        return new EnumerateMessagesStorageResponseDto(response.messages, HttpUtility.UrlEncode(response.continuationToken));
    }

    public async Task<StartConversationResponseDto> StartConversation(StartConversationRequestDto startConversationRequestDto)
    {
        var userId1 = startConversationRequestDto.participants[0];
        var userId2 = startConversationRequestDto.participants[1];
        if (String.IsNullOrWhiteSpace(userId1) ||
            String.IsNullOrWhiteSpace(userId2))
        {
            throw new ArgumentException("Invalid user ID");
        }
        await _profileStorage.GetProfile(userId1);
        await _profileStorage.GetProfile(userId2);

        var conversation = new Conversation(userId1, userId2,
            DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        try
        {
            await _conversationStorage.GetConversation(conversation.userId1, conversation.userId2);
            throw new ConversationConflictException(
                $"There already exists a conversation between {userId1} and {userId2}");
        }
        catch (ConversationNotFoundException)
        {
            string conversationId = await _conversationStorage.PostConversation(conversation);

            var sendMessageRequest = startConversationRequestDto.firstMessage;
            var message = new Message(
                sendMessageRequest.messageId,
                sendMessageRequest.text,
                sendMessageRequest.senderUsername,
                conversationId,
                conversation.lastModifiedUnixTime
            );
            if (String.IsNullOrWhiteSpace(message.conversationId) ||
                String.IsNullOrWhiteSpace(message.text) ||
                String.IsNullOrWhiteSpace(message.senderUsername) ||
                String.IsNullOrWhiteSpace(message.messageId))
            {
                throw new ArgumentException($"Invalid message {message}", nameof(message));
            }

            await _messageStorage.PostMessageToConversation(message);
            return new StartConversationResponseDto(conversationId, conversation.lastModifiedUnixTime);
        }
    }

    public async Task<EnumerateConversationsStorageResponseDto> EnumerateConversationsOfAGivenUser(   
        string userId,
        string? continuationToken = null,
        int? limit = null,
        long? lastSeenConversationTime = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException($"Invalid user ID");
        }

        await _profileStorage.GetProfile(userId);
        var response =
            await _conversationStorage.EnumerateConversationsForAGivenUser(userId, continuationToken, limit,
                lastSeenConversationTime);
        return new EnumerateConversationsStorageResponseDto(response.conversations,
            HttpUtility.UrlEncode(response.continuationToken));
    }
}