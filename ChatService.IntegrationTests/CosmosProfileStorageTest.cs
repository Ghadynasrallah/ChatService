using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ChatService.Dtos;
using ChatService.Storage;

namespace ChatService.Integrationtests;

public class CosmosProfileStorageTest : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly IProfileStorage _store;

    private readonly Profile _profile = new(
        username: Guid.NewGuid().ToString(),
        firstName: "Foo",
        lastName: "Bar", 
        profilePictureId: Guid.NewGuid().ToString()
    );
    
    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _store.DeleteProfile(_profile.username);
    }

    public CosmosProfileStorageTest(WebApplicationFactory<Program> factory)
    {
        _store = factory.Services.GetRequiredService<IProfileStorage>();
    }
    
    [Fact]
    public async Task AddNewProfile()
    {
        await _store.UpsertProfile(_profile);
        Assert.Equal(_profile,  await _store.GetProfile(_profile.username));
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
    public async Task AddInvalidProfile(string username, string firstName, string lastName)
    {
        var profile = new Profile(username, firstName, lastName, Guid.NewGuid().ToString());
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _store.UpsertProfile(profile);
        });
    }

    [Fact] 
    public async Task GetNonExistingProfile()
    {
        Assert.Null(await _store.GetProfile(_profile.username));
    }

    [Fact]
    public async Task UpdateProfile()
    {
        await _store.UpsertProfile(_profile);
        var newProfile = new Profile(_profile.username, "Bar", "Foo", "newId");
        await _store.UpsertProfile(newProfile);
        Assert.Equal(newProfile, await _store.GetProfile(_profile.username));
    }

    [Fact]
    public async Task DeleteProfile()
    {
        await _store.UpsertProfile(_profile);
        Assert.Equal(_profile, await _store.GetProfile(_profile.username));
        await _store.DeleteProfile(_profile.username);
        Assert.Null(await _store.GetProfile(_profile.username));
    }

    [Fact]
    public async Task DeleteNonExistingProfile()
    {
        await _store.DeleteProfile("non-existing");
    }
}
