using System.Text;
using ChatService.Exceptions;
using ChatService.Services;
using ChatService.Storage;
using Moq;

namespace ChatService.Tests;

public class ImageServiceTest : IClassFixture<ImageServiceTest>
{
    private readonly Mock<IProfilePictureStorage> _profilePictureStorageMock = new();
    private readonly ImageService _imageService;
    private readonly Stream _testStream = new MemoryStream(Encoding.UTF8.GetBytes("Hello world!"));
    public ImageServiceTest()
    {
        _imageService = new ImageService(_profilePictureStorageMock.Object);
    }

    [Fact]
    public async Task UploadImage_Success()
    {
        string imageId = await _imageService.UploadImage(_testStream);
        Assert.True(Guid.TryParse(imageId, out _));
        _profilePictureStorageMock.Verify(m=>m.UploadImage(imageId, _testStream), Times.Once);
    }

    [Fact]
    public async Task UploadImage_EmptyStream()
    {
        Stream emptyStream = new MemoryStream(Encoding.UTF8.GetBytes(""));
        await Assert.ThrowsAsync<ArgumentException>(() => _imageService.UploadImage(emptyStream));
    }

    [Fact]
    public async Task DownloadImage_Success()
    {
        string imageId = Guid.NewGuid().ToString();
        _profilePictureStorageMock.Setup(m => m.DownloadImage(imageId)).ReturnsAsync(_testStream);
        
        Assert.Equal(await _imageService.DownloadImage(imageId), _testStream);
        _profilePictureStorageMock.Verify(m=>m.DownloadImage(imageId), Times.Once);
    }

    [Fact]
    public async Task DownloadImage_ImageDoesNotExist()
    {
        string imageId = Guid.NewGuid().ToString();
        _profilePictureStorageMock.Setup(m => m.DownloadImage(imageId)).ReturnsAsync((Stream?)null);

        await Assert.ThrowsAsync<ImageNotFoundException>(() => _imageService.DownloadImage(imageId));
        _profilePictureStorageMock.Verify(m=>m.DownloadImage(imageId), Times.Once);
    }

    [Theory]
    [InlineData("  ")]
    [InlineData("")]
    [InlineData(null)]
    public async Task DownloadImage_InvalidArgs(string imageId)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _imageService.DownloadImage(imageId));
        _profilePictureStorageMock.Verify(m=>m.DownloadImage(imageId), Times.Never);
    }
}