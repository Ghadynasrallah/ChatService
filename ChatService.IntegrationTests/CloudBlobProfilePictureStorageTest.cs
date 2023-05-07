using System.Text;
using ChatService.Storage;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ChatService.Integrationtests;

public class CloudBlobProfilePictureStorageTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly IProfilePictureStorage _profilePictureStorage;
    private readonly Stream _imageStream = new MemoryStream(Encoding.UTF8.GetBytes("Hello world!"));

    public CloudBlobProfilePictureStorageTest(WebApplicationFactory<Program> factory)
    {
        _profilePictureStorage = factory.Services.GetRequiredService<IProfilePictureStorage>();
    }
    
    [Fact]
    public async void UploadAndDownloadValidImage()
    {
        var guid = Guid.NewGuid().ToString();
        await _profilePictureStorage.UploadImage(guid, _imageStream);
        var downloadedStream = await _profilePictureStorage.DownloadImage(guid);
        
        Assert.Equal(_imageStream.ToString(), downloadedStream?.ToString());
        await _profilePictureStorage.DeleteImage(guid);
    }

    [Fact]
    public async void DeleteValidImage()
    {
        var guid = Guid.NewGuid().ToString();
        await _profilePictureStorage.UploadImage(guid, _imageStream);
        var downloadedStream = await _profilePictureStorage.DownloadImage(guid);
        
        Assert.Equal(_imageStream.ToString(), downloadedStream.ToString());

        await _profilePictureStorage.DeleteImage(guid);
        Assert.Null(await _profilePictureStorage.DownloadImage(guid));
    }
}
