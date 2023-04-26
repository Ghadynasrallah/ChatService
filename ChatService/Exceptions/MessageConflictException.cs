namespace ChatService.Exceptions;

public class MessageConflictException : Exception
{
    public MessageConflictException()
    {
    }

    public MessageConflictException(string message) : base(message)
    {
    }

    public MessageConflictException(string message, Exception innerException) : base(message, innerException)
    {
    }
}