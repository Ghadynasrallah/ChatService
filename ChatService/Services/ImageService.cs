using ChatService.Dtos;
using ChatService.Exceptions;
using ChatService.Storage;

namespace ChatService.Services;

public class ImageService : IImageService
{
    private readonly IProfilePictureStorage _profilePictureStorage;

    public ImageService(IProfilePictureStorage profilePictureStorage)
    {
        _profilePictureStorage = profilePictureStorage;
    }

    public async Task<string> UploadImage(Stream profilePictureData)
    {
        if (profilePictureData.Length == 0)
        {
            throw new ArgumentException("Image file is empty");
        }

        string imageId = Guid.NewGuid().ToString();
        await _profilePictureStorage.UploadImage(imageId, profilePictureData);
        return imageId;
    }

    public async Task<Stream> DownloadImage(string imageId)
    {
        if (String.IsNullOrWhiteSpace(imageId))
        {
            throw new ArgumentException("Invalid Arguments: image ID cannot be null or empty");
        }
        var imageData = await _profilePictureStorage.DownloadImage(imageId);
        if (imageData == null)
        {
            throw new ImageNotFoundException($"There exists no image with ID {imageId}");
        }
        return imageData;
    }
}