namespace ChatService.Exceptions;

public class UserConflictException : Exception
{
    public UserConflictException()
    {
    }

    public UserConflictException(string message) : base(message)
    {
    }
}