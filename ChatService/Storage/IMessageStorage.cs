using ChatService.Dtos;

namespace ChatService.Storage;

public interface IMessageStorage
{
    public Task<EnumerateMessagesStorageResponseDto?> EnumerateMessagesFromAGivenConversation(string conversationId,
        string? continuationToken = null,
        int? limit = null,
        long? lastSeenMessageTime = null);

    public Task PostMessageToConversation(Message message);

    public Task<Message?> GetMessage(string conversationId, string messageId);

    public Task<bool> DeleteMessage(string conversationId, string messageId);
}