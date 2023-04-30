namespace ChatService.Exceptions;

public class MessageNotFoundException : Exception
{
    public MessageNotFoundException()
    {
    }

    public MessageNotFoundException(string message) : base(message)
    {
    }
}