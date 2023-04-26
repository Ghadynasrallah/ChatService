namespace ChatService.Exceptions;

public class SenderNotParticipantException : Exception
{
    public SenderNotParticipantException()
    {
    }

    public SenderNotParticipantException(string message) : base(message)
    {
    }

    public SenderNotParticipantException(string message, Exception innerException) : base(message, innerException)
    {
    }
}