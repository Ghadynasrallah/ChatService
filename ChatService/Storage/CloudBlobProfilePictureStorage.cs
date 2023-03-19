using System.Runtime.InteropServices.ComTypes;
using Azure.Storage.Blobs;
using ChatService.Dtos;
using Microsoft.Azure.Storage.Blob;

namespace ChatService.Storage;

public class CloudBlobProfilePictureStorage : IProfilePictureStorage
{
    private readonly BlobServiceClient _blobServiceClient;

    public CloudBlobProfilePictureStorage(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient;
    }

    private BlobContainerClient _blobContainerClient => _blobServiceClient.GetBlobContainerClient("profile-pictures");
    public async Task<string> UploadImage(Stream ProfilePicturedata)
    {
        if (ProfilePicturedata.Length == 0)
        {
            throw new ArgumentException("Image file is empty");
        }
        string guid = Guid.NewGuid().ToString();
        BlobClient blobClient = _blobContainerClient.GetBlobClient(guid);
        
        while (await blobClient.ExistsAsync())
        {
            guid = Guid.NewGuid().ToString();
            blobClient = _blobContainerClient.GetBlobClient(guid);
        }

        await blobClient.UploadAsync(ProfilePicturedata);
        return guid;
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
        BlobClient blobClient = _blobContainerClient.GetBlobClient(profilePictureId);
        await blobClient.DeleteIfExistsAsync();
    }
}