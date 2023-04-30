namespace ChatService.Exceptions;

public class ImageNotFoundException : Exception
{
    public ImageNotFoundException()
    {
    }

    public ImageNotFoundException(string message) : base(message)
    {
    }
}