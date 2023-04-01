using ChatService.Dtos;

namespace ChatService.Storage;

public interface IMessageStorage
{
    public Task<List<Message>> EnumerateMessagesFromAGivenConversation(String conversationId);

    public Task PostMessageToConversation(Message message);
}