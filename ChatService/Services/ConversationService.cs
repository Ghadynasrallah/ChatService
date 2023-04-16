using System.Web;
using ChatService.Dtos;
using ChatService.Exceptions;
using ChatService.Storage;

namespace ChatService.Services;

public class ConversationService : IConversationService
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
        if (String.IsNullOrWhiteSpace(sendMessageRequest.Text) ||
            String.IsNullOrWhiteSpace(sendMessageRequest.SenderUsername) ||
            String.IsNullOrWhiteSpace(sendMessageRequest.MessageId))
        {
            throw new ArgumentException($"Invalid message {sendMessageRequest}", nameof(sendMessageRequest));
        }

        var conversation = await _conversationStorage.GetConversation(conversationId);
        if (conversation == null)
        {
            throw new ConversationNotFoundException($"There exists no conversation with ID {conversationId}");
        }
        
        var message = new Message(
            sendMessageRequest.MessageId,
            sendMessageRequest.Text,
            sendMessageRequest.SenderUsername,
            conversationId,
            DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        );
        
        await _messageStorage.PostMessageToConversation(message);
        await _conversationStorage.UpsertConversation(new PostConversationRequest(conversation.UserId1,
            conversation.UserId2, message.unixTime));
        return message;
    }

    public async Task<ListMessagesStorageResponseDto> EnumerateMessagesInAConversation(
        string conversationId,
        string? continuationToken = null,
        int? limit = null,
        long? lastSeenMessageTime = null)
    {
        if (String.IsNullOrWhiteSpace(conversationId))
        {
            throw new ArgumentException($"Invalid conversation ID {conversationId}");
        }

        if (await _conversationStorage.GetConversation(conversationId) == null)
        {
            throw new ConversationNotFoundException($"There exists no conversation with ID {conversationId}");
        }
        var response = await _messageStorage.EnumerateMessagesFromAGivenConversation(conversationId,
            continuationToken, limit, lastSeenMessageTime);
        if (response == null)
        {
            throw new MessageNotFoundException(
                $"There are no messages for the conversation with conversation ID {conversationId}");
        }

        return response;
    }

    public async Task<StartConversationResponse> StartConversation(StartConversationRequest startConversationRequestDto)
    {
        var userId1 = startConversationRequestDto.Participants[0];
        var userId2 = startConversationRequestDto.Participants[1];
        if (String.IsNullOrWhiteSpace(userId1) ||
            String.IsNullOrWhiteSpace(userId2))
        {
            throw new ArgumentException("Invalid user ID");
        }

        if (await _profileStorage.GetProfile(userId1) == null)
        {
            throw new UserNotFoundException($"The user with username {userId1} was not found");
        }
        if (await _profileStorage.GetProfile(userId2) == null)
        {
            throw new UserNotFoundException($"The user with username {userId2} was not found");
        }
        
        var conversation = new PostConversationRequest(userId1, userId2,
            DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        if (await _conversationStorage.GetConversation(conversation.UserId1, conversation.UserId2) == null) {
            throw new ConversationConflictException(
                $"There already exists a conversation between {userId1} and {userId2}");
        }

        string conversationId = await _conversationStorage.UpsertConversation(conversation);

        var sendMessageRequest = startConversationRequestDto.FirstMessage;
        var message = new Message(
            sendMessageRequest.MessageId,
            sendMessageRequest.Text,
            sendMessageRequest.SenderUsername,
            conversationId,
            conversation.LastModifiedUnixTime
        );
        if (String.IsNullOrWhiteSpace(message.conversationId) ||
            String.IsNullOrWhiteSpace(message.text) ||
            String.IsNullOrWhiteSpace(message.senderUsername) ||
            String.IsNullOrWhiteSpace(message.messageId))
        {
            throw new ArgumentException($"Invalid message {message}", nameof(message));
        }

        await _messageStorage.PostMessageToConversation(message);
        string[] participants = new[] { userId1, userId2 };
        return new StartConversationResponse(conversationId,participants, conversation.LastModifiedUnixTime);
    }

    public async Task<ListConversationsServiceResponse> EnumerateConversationsOfAGivenUser(   
        string userId,
        string? continuationToken = null,
        int? limit = null,
        long? lastSeenConversationTime = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException($"Invalid user ID");
        }

        if (await _profileStorage.GetProfile(userId) == null)
        {
            throw new UserNotFoundException($"The user with username {userId} was not found");
        }
        var response =
            await _conversationStorage.EnumerateConversationsForAGivenUser(userId, continuationToken, limit,
                lastSeenConversationTime);
        if (response == null)
        {
            throw new ConversationNotFoundException($"There exists no conversations for the user with ID {userId}");
        }

        var conversations = response.Conversations;
        List<ListConversationsResponseItem> userConversations = new List<ListConversationsResponseItem>();
        foreach (var conversation in conversations)
        {
            userConversations.Add(await ToUserConversation(conversation, userId));
        }

        return new ListConversationsServiceResponse(userConversations, response.ContinuationToken);
    }

    private async Task<ListConversationsResponseItem> ToUserConversation(Conversation conversation, string userId)
    {
        Profile? recipient;
        if (conversation.UserId1 != userId)
        {
            recipient = await _profileStorage.GetProfile(conversation.UserId1);
        }
        else
        {
            recipient = await _profileStorage.GetProfile(conversation.UserId2);
        }

        return new ListConversationsResponseItem(conversation.ConversationId, conversation.LastModifiedUnixTime, recipient);
    }
}