using Azure.Storage.Blobs;
using Stream = System.IO.Stream;

namespace ChatService.Storage;

public class CloudBlobProfilePictureStorage : IProfilePictureStorage
{
    private readonly BlobServiceClient _blobServiceClient;

    public CloudBlobProfilePictureStorage(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient;
    }

    private BlobContainerClient _blobContainerClient => _blobServiceClient.GetBlobContainerClient("profile-pictures");
    public async Task UploadImage(string imageId, Stream profilePicturedata)
    {
        BlobClient blobClient = _blobContainerClient.GetBlobClient(imageId);
        await blobClient.UploadAsync(profilePicturedata);
    }

    public async Task<Stream?> DownloadImage(string profilePictureId)
    {
        BlobClient blobClient = _blobContainerClient.GetBlobClient(profilePictureId);

        if (! await blobClient.ExistsAsync())
        {
            return null;
        }

        Stream imageData = new MemoryStream();
        var downloadResponse = await blobClient.DownloadAsync();
        await downloadResponse.Value.Content.CopyToAsync(imageData);
        imageData.Position = 0;
        return imageData;
    }

    public async Task DeleteImage(string profilePictureId)
    {
        if (String.IsNullOrWhiteSpace(profilePictureId))
        {
            throw new ArgumentException("The profile picture ID is invalid: ID does not contain any text");
        }
        BlobClient blobClient = _blobContainerClient.GetBlobClient(profilePictureId);
        await blobClient.DeleteIfExistsAsync();
    }
}