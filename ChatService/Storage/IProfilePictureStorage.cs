using ChatService.Dtos;

namespace ChatService.Storage;

public interface IProfilePictureStorage
{
    Task<string> UploadImage(Stream profilePictureData);
    
    Task<Stream?> DownloadImage(string profilePictureId);

    Task DeleteImage(string profilePictureId);
}
