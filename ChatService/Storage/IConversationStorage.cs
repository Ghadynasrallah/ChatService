using ChatService.Dtos;

namespace ChatService.Storage;

public interface IConversationStorage
{
    public Task<List<Conversation>?> EnumerateConversationsForAGivenUser(string userId);

    public Task<string> PostConversation(Conversation conversation);

    public Task<Conversation?> GetConversation(string userId1, string userId2);

    public Task<bool> DeleteConversation(string userId1, string userId2);
}