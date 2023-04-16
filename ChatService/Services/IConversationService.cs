using ChatService.Dtos;

namespace ChatService.Services;

public interface IConversationService
{
    public Task<Message> SendMessageToConversation(string conversationId, SendMessageRequest sendMessageRequest);

    public Task<ListMessagesStorageResponseDto> EnumerateMessagesInAConversation(
        string conversationId,
        string? continuationToken = null,
        int? limit = null,
        long? lastSeenMessageTime = null);

    public Task<StartConversationResponse> StartConversation(StartConversationRequest startConversationRequestDto);

    public Task<ListConversationsServiceResponse> EnumerateConversationsOfAGivenUser(
        string userId,
        string? continuationToken = null,
        int? limit = null,
        long? lastSeenConversationTime = null);
}