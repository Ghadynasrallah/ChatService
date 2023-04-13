using ChatService.Dtos;

namespace ChatService.Storage;

public interface IConversationStorage
{
    public Task<EnumerateConversationsStorageResponseDto?> EnumerateConversationsForAGivenUser(string userId,
        string? continuationToken = null,
        int? limit = null,
        long? lastSeenMessageTime = null);

    public Task<string> PostConversation(Conversation conversation);

    public Task<Conversation?> GetConversation(string userId1, string userId2);

    public Task<bool> DeleteConversation(string userId1, string userId2);
}