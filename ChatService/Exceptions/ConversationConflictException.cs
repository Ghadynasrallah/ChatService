namespace ChatService.Exceptions;

public class ConversationConflictException : Exception
{
    public ConversationConflictException()
    {
    }

    public ConversationConflictException(string message) : base(message)
    {
    }
}