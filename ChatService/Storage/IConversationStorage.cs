using ChatService.Dtos;

namespace ChatService.Storage;

public interface IConversationStorage
{
    public Task<List<Conversation>?> EnumerateConversationsForAGivenUser(string userId);

    public Task PostConversation(Conversation conversation);

    public Task<Conversation?> GetConversation(String conversationId);

    public Task<bool> DeleteConversation(String conversationId);
}