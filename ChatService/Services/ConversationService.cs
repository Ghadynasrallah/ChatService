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
        var conversation = await ValidateConversation(conversationId);
        await ValidateSendMessage(conversation, sendMessageRequest);
        var message = new Message(
            sendMessageRequest.Id,
            sendMessageRequest.Text,
            sendMessageRequest.SenderUsername,
            conversationId,
            DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        );
        await _messageStorage.PostMessageToConversation(message);
        await _conversationStorage.UpsertConversation(new PostConversationRequest(conversation.UserId1,
            conversation.UserId2, message.UnixTime));
        return message;
    }

    public async Task<ListMessageServiceResponseDto> EnumerateMessagesInAConversation(
        string conversationId,
        string? continuationToken = null,
        int? limit = null,
        long? lastSeenMessageTime = null)
    {
        await ValidateConversation(conversationId);
        var response = await _messageStorage.EnumerateMessagesFromAGivenConversation(conversationId,
            continuationToken, limit, lastSeenMessageTime);
        
        List<ListMessageResponseItem> messagesResult = new List<ListMessageResponseItem>();
        foreach (var message in response.Messages)
        {
            messagesResult.Add(ToMessageResponse(message));
        }
        return new ListMessageServiceResponseDto(messagesResult, response.ContinuationToken);
    }

    public async Task<AddConversationResponse> StartConversation(AddConversationRequest startConversationRequest)
    {
        await ValidateAddNewConversation(startConversationRequest);
        
        var userId1 = startConversationRequest.Participants[0];
        var userId2 = startConversationRequest.Participants[1];
        var sendMessageRequest = startConversationRequest.FirstMessage;

        await ValidateUserProfile(userId1);
        await ValidateUserProfile(userId2);

        var postConversation = new PostConversationRequest(userId1, userId2, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        string conversationId = await _conversationStorage.UpsertConversation(postConversation);
        var conversation = new Conversation(conversationId, userId1, userId2, postConversation.LastModifiedUnixTime);
        
        await ValidateSendMessage(conversation, sendMessageRequest);
        var message = new Message(
            sendMessageRequest.Id,
            sendMessageRequest.Text,
            sendMessageRequest.SenderUsername,
            conversationId,
            conversation.LastModifiedUnixTime
        );
        await _messageStorage.PostMessageToConversation(message);
        return new AddConversationResponse(conversationId, startConversationRequest.Participants, DateTimeOffset.FromUnixTimeSeconds(conversation.LastModifiedUnixTime).DateTime);
    }

    public async Task<ListConversationsServiceResponse> EnumerateConversationsOfAGivenUser(   
        string userId,
        string? continuationToken = null,
        int? limit = null,
        long? lastSeenConversationTime = null)
    {
        await ValidateUserProfile(userId);
        var response =
            await _conversationStorage.EnumerateConversationsForAGivenUser(userId, continuationToken, limit,
                lastSeenConversationTime);

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
    
    private static ListMessageResponseItem ToMessageResponse(Message message)
    {
        return new ListMessageResponseItem(message.Text, message.SenderUsername, message.UnixTime);
    }

    private async Task ValidateSendMessage(Conversation conversation, SendMessageRequest sendMessageRequest)
    {
        if (String.IsNullOrWhiteSpace(sendMessageRequest.Text) ||
            String.IsNullOrWhiteSpace(sendMessageRequest.SenderUsername) ||
            String.IsNullOrWhiteSpace(sendMessageRequest.Id))
        {
            throw new ArgumentException($"Invalid message {sendMessageRequest}", nameof(sendMessageRequest));
        }
        if (sendMessageRequest.SenderUsername != conversation.UserId1 &&
            sendMessageRequest.SenderUsername != conversation.UserId2)
        {
            throw new SenderNotParticipantException(
                $"The sender with username {sendMessageRequest.SenderUsername} is not a participant of the conversation with ID {conversation.ConversationId}");
        }
        if (await _messageStorage.GetMessage(conversation.ConversationId, sendMessageRequest.Id) != null)
        {
            throw new MessageConflictException(
                $"There already exists a message with id {sendMessageRequest.Id} in conversation {conversation.ConversationId}");
        }
    }

    private async Task<Conversation> ValidateConversation(string conversationId)
    {
        if (String.IsNullOrWhiteSpace(conversationId))
        {
            throw new ArgumentException($"Invalid conversation ID {conversationId}");
        }
        var conversation = await _conversationStorage.GetConversation(conversationId);
        if (conversation == null)
        {
            throw new ConversationNotFoundException($"There exists no conversation with ID {conversationId}");
        }
        return conversation;
    }

    private async Task<Profile> ValidateUserProfile(string username)
    {
        if (String.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Invalid username: username cannot be null or empty");
        }

        var profile = await _profileStorage.GetProfile(username);
        if (profile == null)
        {
            throw new UserNotFoundException($"The user with username {username} was not found");
        }
        return profile;
    }

    private async Task ValidateAddNewConversation(AddConversationRequest addConversationRequest)
    {
        if (addConversationRequest.Participants.Length != 2)
        {
            throw new ArgumentException("There must be 2 participants");
        }

        string userId1 = addConversationRequest.Participants[0];
        string userId2 = addConversationRequest.Participants[1];
        var conversation = await _conversationStorage.GetConversation(userId1, userId2);
        if (conversation != null) {
            throw new ConversationConflictException($"There already exists a conversation between {userId1} and {userId2}");
        }
    }
}