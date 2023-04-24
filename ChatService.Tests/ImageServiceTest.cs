using ChatService.Services;
using ChatService.Storage;
using Moq;

namespace ChatService.Tests;

public class ImageServiceTest : IClassFixture<ImageServiceTest>
{
    private readonly Mock<IProfilePictureStorage> _profilePictureStorageMock;
    private readonly ImageService _imageService;

    public ImageServiceTest()
    {
        _imageService = new ImageService(_profilePictureStorageMock.Object);
    } 
}