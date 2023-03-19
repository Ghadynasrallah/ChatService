using System.Net;
using ChatService.Dtos;
using ChatService.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using ContentDispositionHeaderValue = System.Net.Http.Headers.ContentDispositionHeaderValue;

namespace ChatService.Tests;

public class ImageControllerTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly Mock<IProfilePictureStorage> _profilePictureStorageMock = new();
    private readonly HttpClient _httpClient;
    private readonly MemoryStream EmptyStream = new MemoryStream(new byte[0]);
    private readonly MemoryStream ImageStream = new MemoryStream(File.ReadAllBytes(@"/Users/ghady/RiderProjects/TestImage.jpg"));

    public ImageControllerTest(WebApplicationFactory<Program> factory)
    {
        _httpClient = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services => { services.AddSingleton(_profilePictureStorageMock.Object); });
        }).CreateClient();
    }

    [Fact]
    public async Task UploadValidImage()
    {
        HttpContent fileStreamContent = new StreamContent(ImageStream);
        fileStreamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
        {
            Name = "file",
            FileName = "Image"
        };
        using var formData = new MultipartFormDataContent();
        formData.Add(fileStreamContent);

        var guid = Guid.NewGuid().ToString();
        _profilePictureStorageMock.Setup(m => m.UploadImage(It.IsAny<Stream>()))
            .ReturnsAsync(guid);

        var response = await _httpClient.PostAsync("Image", formData);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();

        Assert.Equal(guid, JsonConvert.DeserializeObject<UploadImageResponse>(json).imageId);

        _profilePictureStorageMock.Verify(m=> m.UploadImage(It.IsAny<Stream>()), Times.Once);
    }

    [Fact]
    public async Task DownloadValidImage()
    {
        var guid = Guid.NewGuid().ToString();
        _profilePictureStorageMock.Setup(m => m.DownloadImage(guid))
            .ReturnsAsync(ImageStream);

        var response = await _httpClient.GetAsync($"Image/{guid}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        _profilePictureStorageMock.Verify(m =>m.DownloadImage(guid));

        var responseContent = new FileContentResult(await response.Content.ReadAsByteArrayAsync(), "image/jpeg");
        var expectedContent = new FileContentResult(ImageStream.ToArray(), "image/jpeg");

        Assert.Equal(responseContent, expectedContent);
    }

    [Fact]
    public async Task DownloadNonValidImage()
    {
        var guid = Guid.NewGuid().ToString();
        _profilePictureStorageMock.Setup(m => m.DownloadImage(guid))
            .ReturnsAsync((Stream)null);
        
        var response = await _httpClient.GetAsync($"Image/{guid}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}