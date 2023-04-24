using System.Drawing;
using System.Net;
using System.Text;
using ChatService.Dtos;
using ChatService.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Azure.Cosmos.Core;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using ContentDispositionHeaderValue = System.Net.Http.Headers.ContentDispositionHeaderValue;

namespace ChatService.Tests;

public class ImageControllerTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly Mock<IProfilePictureStorage> _profilePictureStorageMock = new();
    private readonly HttpClient _httpClient;
    private readonly MemoryStream _emptyStream = new MemoryStream(new byte[0]);
    //private readonly MemoryStream ImageStream = new MemoryStream(File.ReadAllBytes(@"/Users/ghady/RiderProjects/TestImage.jpg"));
    private readonly MemoryStream testStream = new MemoryStream(Encoding.UTF8.GetBytes("Hello world"));

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
        HttpContent fileStreamContent = new StreamContent(testStream);
        fileStreamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
        {
            Name = "file",
            FileName = "Image"
        };
        using var formData = new MultipartFormDataContent();
        formData.Add(fileStreamContent);

        var guid = Guid.NewGuid().ToString();
        _profilePictureStorageMock.Setup(m => m.UploadImage(guid, It.IsAny<Stream>()));

        var response = await _httpClient.PostAsync("Image", formData);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();

        Assert.Equal(guid, JsonConvert.DeserializeObject<UploadImageResponse>(json).ImageId);

        _profilePictureStorageMock.Verify(m=> m.UploadImage(guid, It.IsAny<Stream>()), Times.Once);
    }

    [Fact]
    public async Task DownloadValidImage()
    {
        // Arrange
        HttpContent fileStreamContent = new StreamContent(testStream); 
        fileStreamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
        {
            Name = "file",
            FileName = "anything" // this is not important but must not be empty
        };

        using var formData = new MultipartFormDataContent();
        formData.Add(fileStreamContent);

        var guid = Guid.NewGuid().ToString();
        testStream.Position = 0;
        _profilePictureStorageMock.Setup(m => m.DownloadImage(guid))
            .ReturnsAsync(testStream);

        var expectedData = testStream.ToArray();

        // Act
        var response = await _httpClient.GetAsync($"Image/{guid}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal(expectedData, responseContent);
        _profilePictureStorageMock.Verify(m => m.DownloadImage(guid), Times.Once);
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
