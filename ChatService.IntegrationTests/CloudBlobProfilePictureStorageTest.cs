using System.IO.Pipelines;
using System.Text;
using ChatService.Dtos;
using ChatService.Storage;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ChatService.Integrationtests;

public class CloudBlobProfilePictureStorageTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly IProfilePictureStorage _IProfilePictureStorage;

    private readonly MemoryStream EmptyStream = new MemoryStream(new byte[0]);
    private readonly FileStream ImageStream = File.OpenRead(@"/Users/ghady/RiderProjects/TestImage.jpg");
    private readonly byte[] ImageStreamContent = File.ReadAllBytes(@"/Users/ghady/RiderProjects/TestImage.jpg");

    public CloudBlobProfilePictureStorageTest(WebApplicationFactory<Program> factory)
    {
        _IProfilePictureStorage = factory.Services.GetRequiredService<IProfilePictureStorage>();
    }
    
    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync(string guid)
    {
        _IProfilePictureStorage.DeleteImage(guid);
    }

    [Fact]
    public async void UploadAndDownloadValidImage()
    {
        var guid = await _IProfilePictureStorage.UploadImage(ImageStream);
        var blobStream = await _IProfilePictureStorage.DownloadImage(guid);

        Assert.Equal(ImageStreamContent, ConvertStreamToByteArray(blobStream));
        await _IProfilePictureStorage.DeleteImage(guid);
    }

    [Fact]
    public async void UploadEmptyImage()
    {
        Assert.ThrowsAsync<ArgumentException>(async () => await _IProfilePictureStorage.UploadImage(EmptyStream));
    }

    [Fact]
    public async void DeleteValidImage()
    {
        var guid = await _IProfilePictureStorage.UploadImage(ImageStream);
        var blobStream = await _IProfilePictureStorage.DownloadImage(guid);
        
        Assert.Equal(ImageStreamContent, ConvertStreamToByteArray(blobStream));

        await _IProfilePictureStorage.DeleteImage(guid);
        Assert.Null(await _IProfilePictureStorage.DownloadImage(guid));
    }

    public byte[] ConvertStreamToByteArray(Stream stream)
    {
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }
}
