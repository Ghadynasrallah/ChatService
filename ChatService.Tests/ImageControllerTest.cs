using System.Net;
using System.Text;
using ChatService.Dtos;
using ChatService.Exceptions;
using ChatService.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using ContentDispositionHeaderValue = System.Net.Http.Headers.ContentDispositionHeaderValue;

namespace ChatService.Tests;

public class ImageControllerTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly Mock<IImageService> _imageServiceMock = new();
    private readonly HttpClient _httpClient;
    private readonly MemoryStream _testStream = new MemoryStream(Encoding.UTF8.GetBytes("Hello world!"));

    public ImageControllerTest(WebApplicationFactory<Program> factory)
    {
        _httpClient = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services => { services.AddSingleton(_imageServiceMock.Object); });
        }).CreateClient();
    }

    [Fact]
    public async Task UploadValidImage()
    {
        HttpContent fileStreamContent = new StreamContent(_testStream);
        fileStreamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
        {
            Name = "file",
            FileName = "Image"
        };
        using var formData = new MultipartFormDataContent();
        formData.Add(fileStreamContent);

        var guid = Guid.NewGuid().ToString();
        _imageServiceMock.Setup(m => m.UploadImage(It.IsAny<Stream>())).ReturnsAsync(guid);

        var response = await _httpClient.PostAsync("api/images", formData);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();

        Assert.Equal(guid, JsonConvert.DeserializeObject<UploadImageResponse>(json).ImageId);

        _imageServiceMock.Verify(m=> m.UploadImage(It.IsAny<Stream>()), Times.Once);
    }

    [Fact]
    public async Task UploadImage_BadRequest()
    {
        HttpContent fileStreamContent = new StreamContent(_testStream);
        fileStreamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
        {
            Name = "file",
            FileName = "Image"
        };
        using var formData = new MultipartFormDataContent();
        formData.Add(fileStreamContent);

        var guid = Guid.NewGuid().ToString();
        _imageServiceMock.Setup(m => m.UploadImage(It.IsAny<Stream>())).ThrowsAsync(new ArgumentException());
        
        var response = await _httpClient.PostAsync("api/images", formData);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DownloadValidImage()
    {
        var guid = Guid.NewGuid().ToString();
        _testStream.Position = 0;
        _imageServiceMock.Setup(m => m.DownloadImage(guid))
            .ReturnsAsync(_testStream);

        var expectedData = _testStream.ToArray();

        // Act
        var response = await _httpClient.GetAsync($"api/images/{guid}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal(expectedData, responseContent);
        _imageServiceMock.Verify(m => m.DownloadImage(guid), Times.Once);
    }
    
    [Fact]
    public async Task DownloadImage_NotFound()
    {
        var guid = Guid.NewGuid().ToString();
        _imageServiceMock.Setup(m => m.DownloadImage(guid)).ThrowsAsync(new ImageNotFoundException());

        var response = await _httpClient.GetAsync($"api/images/{guid}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DownloadImage_BadRequest()
    {
        string id = "test";
        _imageServiceMock.Setup(m => m.DownloadImage(id)).ThrowsAsync(new ArgumentException());
        var response = await _httpClient.GetAsync($"api/images/{id}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
