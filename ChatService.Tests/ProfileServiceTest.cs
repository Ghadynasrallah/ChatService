using System.Runtime.InteropServices;
using ChatService.Dtos;
using ChatService.Exceptions;
using ChatService.Services;
using ChatService.Storage;
using Moq;

namespace ChatService.Tests;

public class ProfileServiceTest : IClassFixture<ProfileServiceTest>
{
    private readonly Mock<IProfileStorage> _profileStorageMock = new();
    private readonly ProfileService _profileService;
    private readonly Profile _profile1 = new Profile("foobar", "foo", "bar", "testID");

    public ProfileServiceTest()
    {
        _profileService = new ProfileService(_profileStorageMock.Object);
    }

    [Fact]
    public async Task GetProfile_Success()
    {
        //Setup
        _profileStorageMock.Setup(m => m.GetProfile(_profile1.Username)).ReturnsAsync(_profile1);
        
        //Assert
        Assert.Equal(_profile1, await _profileService.GetProfile(_profile1.Username));
    }

    [Theory]
    [InlineData("  ")]
    [InlineData(null)]
    [InlineData("")]
    public async Task GetProfile_InvalidArgs(string userId)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _profileService.GetProfile(userId));
    }

    [Fact]
    public async Task GetProfile_ProfileNotFound()
    {
        //Setup
        _profileStorageMock.Setup(m => m.GetProfile(_profile1.Username)).ReturnsAsync((Profile?)null);
        
        //Assert
        await Assert.ThrowsAsync<UserNotFoundException>(() => _profileService.GetProfile(_profile1.Username));
    }

    [Fact]
    public async Task AddProfile_Success()
    {
        //Setup
        _profileStorageMock.Setup(m => m.GetProfile(_profile1.Username)).ReturnsAsync((Profile?)null);
        
        //Verify
        await _profileService.AddProfile(_profile1);
        _profileStorageMock.Verify(m=>m.GetProfile(_profile1.Username));
        _profileStorageMock.Verify(m=>m.UpsertProfile(_profile1));
    }

    [Theory]
    [InlineData("", "foo", "bar")]
    [InlineData("   ", "foo", "bar")]
    [InlineData(null, "foo", "bar")]
    [InlineData("foobar", "  ", "bar")]
    [InlineData("foobar", "", "bar")]
    [InlineData("foobar", null, "bar")]
    [InlineData("foobar", "foo", null)]
    [InlineData("foobar", "foo", "")]
    [InlineData("foobar", "foo", "   ")]
    public async Task AddProfile_InvalidArgs(string userId, string firstName, string lastName)
    {
        //Setup
        var profile = new Profile(userId, firstName, lastName, "testId");
        
        //Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _profileService.AddProfile(profile));
    }
    
    [Fact]
    public async Task AddProfile_UserAlreadyExists()
    {
        //Setup
        _profileStorageMock.Setup(m => m.GetProfile(_profile1.Username)).ReturnsAsync(_profile1);
        
        //Verify
        await Assert.ThrowsAsync<UserConflictException>(() => _profileService.AddProfile(_profile1));
    }

    [Fact]
    public async Task UpdateProfile_Success()
    {
        //Setup
        var putProfileRequest = new PutProfileRequest("jim", "mike", "imageId");
        var updatedProfile = new Profile(_profile1.Username, putProfileRequest.FirstName, putProfileRequest.LastName,
            putProfileRequest.ProfilePictureId);
        _profileStorageMock.Setup(m => m.GetProfile(_profile1.Username)).ReturnsAsync(_profile1);
        
        //Verify
        Assert.Equal(await _profileService.UpdateProfile(_profile1.Username, putProfileRequest), updatedProfile);
        _profileStorageMock.Verify(m=>m.GetProfile(_profile1.Username), Times.Once);
        _profileStorageMock.Verify(m=>m.UpsertProfile(updatedProfile), Times.Once);
    }

    [Theory]
    [InlineData("", "foo", "bar")]
    [InlineData("  ", "foo", "bar")]
    [InlineData(null, "foo", "bar")]
    [InlineData("foobar", "  ", "bar")]
    [InlineData("foobar", "", "bar")]
    [InlineData("foobar", null, "bar")]
    [InlineData("foobar", "foo", "")]
    [InlineData("foobar", "foo", "    ")]
    [InlineData("foobar", "foo", null)]
    public async Task UpdateProfile_InvalidArgs(string username, string firstName, string lastName)
    {
        //Setup
        var putProfileRequest = new PutProfileRequest(firstName, lastName, "testImageId");
        
        //Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _profileService.UpdateProfile(username, putProfileRequest));
    }

    [Fact]
    public async Task UpdateProfile_UserDoesNotExist()
    {
        //Setup
        var putProfileRequest = new PutProfileRequest("jim", "mike", "imageId");
        _profileStorageMock.Setup(m => m.GetProfile(_profile1.Username)).ReturnsAsync((Profile?)null);
        
        //Assert
        await Assert.ThrowsAsync<UserNotFoundException>(() =>
            _profileService.UpdateProfile(_profile1.Username, putProfileRequest));
        _profileStorageMock.Verify(m=>m.GetProfile(_profile1.Username), Times.Once);
    }
}