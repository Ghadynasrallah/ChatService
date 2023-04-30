namespace ChatService.Exceptions;

public class SenderNotParticipantException : Exception
{
    public SenderNotParticipantException()
    {
    }

    public SenderNotParticipantException(string message) : base(message)
    {
    }
}