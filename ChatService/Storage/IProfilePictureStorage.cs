using ChatService.Dtos;

namespace ChatService.Storage;

public interface IProfilePictureStorage
{
    Task UploadImage(string imageId, Stream profilePictureData);
    
    Task<Stream?> DownloadImage(string imageId);

    Task DeleteImage(string imageId);
}
