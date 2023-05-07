namespace ChatService.Services;

public interface IImageService
{
    public Task<string> UploadImage(Stream profilePictureData);

    public Task<Stream> DownloadImage(string imageId);
}