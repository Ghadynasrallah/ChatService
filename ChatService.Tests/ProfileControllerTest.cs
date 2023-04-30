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

namespace ChatService.Tests;

public class UserControllerTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly Mock<IProfileService> _profileServiceMock = new();
    private readonly HttpClient _httpClient;
    private readonly Profile _profile = new Profile("foobar", "Foo", "Bar", Guid.NewGuid().ToString());

    public UserControllerTest(WebApplicationFactory<Program> factory)
    {
        _httpClient = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services => { services.AddSingleton(_profileServiceMock.Object); });
        }).CreateClient();
    }

    [Fact]
    public async Task GetProfile()
    {
        _profileServiceMock.Setup(m => m.GetProfile(_profile.Username))
            .ReturnsAsync(_profile);
        
        var response = await _httpClient.GetAsync($"api/profile/{_profile.Username}");
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Equal(_profile, JsonConvert.DeserializeObject<Profile>(json));

        _profileServiceMock.Verify(m => m.GetProfile(_profile.Username), Times.Once);

    }

    [Fact]
    public async Task GetProfile_NotFound()
    {
        _profileServiceMock.Setup(m => m.GetProfile("foobar")).ThrowsAsync(new UserNotFoundException());

        var response = await _httpClient.GetAsync($"api/profile/foobar");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
    
    [Fact]
    public async Task GetProfile_InvalidArgs()
    {
        _profileServiceMock.Setup(m => m.GetProfile("foobar")).ThrowsAsync(new ArgumentException());

        var response = await _httpClient.GetAsync($"api/profile/foobar");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddProfile()
    {
        var response = await _httpClient.PostAsync($"api/profile",
            new StringContent(JsonConvert.SerializeObject(_profile), Encoding.Default, "application/json"));
        
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("http://localhost/api/profile/foobar", response.Headers.GetValues("Location").First());
        
        _profileServiceMock.Verify(mock => mock.AddProfile(_profile), Times.Once);
    }

    [Fact]
    public async Task AddProfile_Conflict()
    {
        _profileServiceMock.Setup(m => m.AddProfile(_profile))
            .ThrowsAsync(new UserConflictException());

        var response = await _httpClient.PostAsync("api/profile",
            new StringContent(JsonConvert.SerializeObject(_profile), Encoding.Default, "application/json"));
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        
        _profileServiceMock.Verify(m => m.AddProfile(_profile), Times.Once);
    }

    [Fact]
    public async Task AddProfile_InvalidArgs()
    {
        _profileServiceMock.Setup(m => m.AddProfile(_profile))
            .ThrowsAsync(new ArgumentException());
        var response = await _httpClient.PostAsync("api/profile",
            new StringContent(JsonConvert.SerializeObject(_profile), Encoding.Default, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        _profileServiceMock.Verify(mock => mock.AddProfile(_profile), Times.Once);
    }

    [Fact]
    public async Task UpdateProfile()
    {
        //Setup
        var putProfileRequest = new PutProfileRequest("mike", "bar", "testId");
        var updatedProfile = new Profile(_profile.Username, putProfileRequest.FirstName, putProfileRequest.LastName,
            putProfileRequest.ProfilePictureId);

        _profileServiceMock.Setup(m => m.UpdateProfile(_profile.Username, putProfileRequest))
            .ReturnsAsync(updatedProfile);
        
        //Act
        var response = await _httpClient.PutAsync($"api/profile/{_profile.Username}",
            new StringContent(JsonConvert.SerializeObject(putProfileRequest), Encoding.Default, "application/json"));
        var json = await response.Content.ReadAsStringAsync();
        var actualProfileResponse = JsonConvert.DeserializeObject<Profile>(json);

        //Assert and Verify
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(updatedProfile, actualProfileResponse);
        _profileServiceMock.Verify(m=>m.UpdateProfile(_profile.Username, putProfileRequest), Times.Once);
    }

    [Fact]
    public async Task UpdateProfile_NotFound()
    {
        //Setup
        var putProfileRequest = new PutProfileRequest("mike", "bar", "testId");
        _profileServiceMock.Setup(m => m.UpdateProfile(_profile.Username, It.IsAny<PutProfileRequest>()))
            .ThrowsAsync(new UserNotFoundException());
        
        //Act
        var response = await _httpClient.PutAsync($"api/profile/{_profile.Username}",
            new StringContent(JsonConvert.SerializeObject(putProfileRequest), Encoding.Default, "application/json"));

        //Assert and Verify
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        _profileServiceMock.Verify(m=>m.UpdateProfile(_profile.Username, putProfileRequest), Times.Once);
    }

    [Fact]
    public async Task UpdateProfile_InvalidArgs()
    {
        //Setup
        var putProfileRequest = new PutProfileRequest("mike", "bar", "testId");
        _profileServiceMock.Setup(m => m.UpdateProfile(_profile.Username, It.IsAny<PutProfileRequest>()))
            .ThrowsAsync(new ArgumentException());
        
        //Act
        var response = await _httpClient.PutAsync($"api/profile/{_profile.Username}",
            new StringContent(JsonConvert.SerializeObject(putProfileRequest), Encoding.Default, "application/json"));

        //Assert and Verify
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        _profileServiceMock.Verify(m=>m.UpdateProfile(_profile.Username, putProfileRequest), Times.Once);
    }
}