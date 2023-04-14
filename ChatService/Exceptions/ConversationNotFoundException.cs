namespace ChatService.Exceptions;

public class ConversationNotFoundException : Exception
{
    public ConversationNotFoundException()
    {
    }

    public ConversationNotFoundException(string message) : base(message)
    {
    }

    public ConversationNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
