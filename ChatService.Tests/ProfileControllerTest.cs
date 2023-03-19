using System.Net;
using System.Text;
using ChatService.Storage;
using ChatService.Dtos;
using ChatService.Controllers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;

namespace ChatService.Tests;

public class UserControllerTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly Mock<IProfileStorage> _profileStorageMock = new();
    private readonly HttpClient _httpClient;

    public UserControllerTest(WebApplicationFactory<Program> factory)
    {
        _httpClient = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services => { services.AddSingleton(_profileStorageMock.Object); });
        }).CreateClient();
    }

    [Fact]
    public async Task GetProfile()
    {
        var profile = new Profile("foobar", "Foo", "Bar", Guid.NewGuid().ToString());
        _profileStorageMock.Setup(m => m.GetProfile(profile.username))
            .ReturnsAsync(profile);
        
        var response = await _httpClient.GetAsync($"Profile/{profile.username}");
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Equal(profile, JsonConvert.DeserializeObject<Profile>(json));

        //_profileStorageMock.Verify(m => m.GetProfile(profile.Username), Times.Once);

    }

    [Fact]
    public async Task GetProfile_NotFound()
    {
        _profileStorageMock.Setup(m => m.GetProfile("foobar"))
            .ReturnsAsync((Profile?)null);

        var response = await _httpClient.GetAsync($"User/Profile/foobar");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AddProfile()
    {
        var profile = new Profile("foobar", "Foo", "Bar", Guid.NewGuid().ToString());
        var response = await _httpClient.PostAsync($"Profile",
            new StringContent(JsonConvert.SerializeObject(profile), Encoding.Default, "application/json"));
        
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("http://localhost/Profile/foobar", response.Headers.GetValues("Location").First());
        
        _profileStorageMock.Verify(mock => mock.UpsertProfile(profile), Times.Once);
    }

    [Fact]
    public async Task AddProfile_Conflict()
    {
        var profile = new Profile("foobar", "Foo", "Bar", Guid.NewGuid().ToString());
        _profileStorageMock.Setup(m => m.GetProfile(profile.username))
            .ReturnsAsync(profile);

        var response = await _httpClient.PostAsync("/Profile",
            new StringContent(JsonConvert.SerializeObject(profile), Encoding.Default, "application/json"));
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        
        _profileStorageMock.Verify(m => m.UpsertProfile(profile), Times.Never);
    }

    [Theory]
    [InlineData(null, "Foo", "Bar")]
    [InlineData("", "Foo", "Bar")]
    [InlineData(" ", "Foo", "Bar")]
    [InlineData("foobar", null, "Bar")]
    [InlineData("foobar", "", "Bar")]
    [InlineData("foobar", "   ", "Bar")]
    [InlineData("foobar", "Foo", "")]
    [InlineData("foobar", "Foo", null)]
    [InlineData("foobar", "Foo", " ")]
    //Note that we did not add tests for ProfilePictureId as it can be null, empty, or whitespace
    public async Task AddProfile_InvalidArgs(string username, string firstName, string lastName)
    {
        var profile = new Profile(username, firstName, lastName, Guid.NewGuid().ToString());
        var response = await _httpClient.PostAsync("/Profile",
            new StringContent(JsonConvert.SerializeObject(profile), Encoding.Default, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        _profileStorageMock.Verify(mock => mock.UpsertProfile(profile), Times.Never);
    }

    [Fact]
    public async Task UpdateProfile()
    {
        var profile = new Profile("foobar", "Foo", "Bar", Guid.NewGuid().ToString());
        _profileStorageMock.Setup(m => m.GetProfile(profile.username))
            .ReturnsAsync(profile);

        var updatedProfile = profile with { firstName = "Foo2", lastName = "Bar2", profilePictureId = Guid.NewGuid().ToString()};

        var response = await _httpClient.PutAsync($"/Profile/{profile.username}",
            new StringContent(JsonConvert.SerializeObject(updatedProfile), Encoding.Default, "application/json"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _profileStorageMock.Verify(mock => mock.UpsertProfile(updatedProfile));
    }
    
    [Fact]
    public async Task UpdateProfile_NotFound()
    {
        var profile = new Profile("foobar", "Foo", "Bar", Guid.NewGuid().ToString());
        _profileStorageMock.Setup(m => m.GetProfile(profile.username))
            .ReturnsAsync((Profile?)null);
        var putProfileRequest = new PutProfileRequest(profile.firstName, profile.lastName, profile.profilePictureId);

        var response = await _httpClient.PutAsync($"User/{profile.username}",
            new StringContent(JsonConvert.SerializeObject(putProfileRequest), Encoding.Default, "application/json"));
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        _profileStorageMock.Verify(mock => mock.UpsertProfile(It.IsAny<Profile>()), Times.Never);
    }

    [Theory]
    [InlineData("foobar", null, "Bar")]
    [InlineData("foobar", "", "Bar")]
    [InlineData("foobar", "   ", "Bar")]
    [InlineData("foobar", "Foo", "")]
    [InlineData("foobar", "Foo", null)]
    [InlineData("foobar", "Foo", " ")]
    public async Task UpdateProfile_InvalidArgs(string username, string firstName, string lastName)
    {
        var putProfileRequest = new PutProfileRequest(firstName, lastName, Guid.NewGuid().ToString());
        var profile = new Profile("foobar", "Foo", "Bar", Guid.NewGuid().ToString());

        _profileStorageMock.Setup(m => m.GetProfile(profile.username))
            .ReturnsAsync(profile);
        
        var response = await _httpClient.PostAsync($"/Profile/{username}",
            new StringContent(JsonConvert.SerializeObject(putProfileRequest), Encoding.Default, "application/json"));
        
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }
}