namespace ChatService.Exceptions;

public class ConversationNotFoundException : Exception
{
    public ConversationNotFoundException()
    {
    }

    public ConversationNotFoundException(string message) : base(message)
    {
    }
}
