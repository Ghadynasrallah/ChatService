using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ChatService.Dtos;
using ChatService.Storage;

namespace ChatService.Integrationtests;

public class CosmosProfileStorageTest : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly IProfileStorage _store;

    private readonly Profile _profile = new(
        Username: Guid.NewGuid().ToString(),
        FirstName: "Foo",
        LastName: "Bar", 
        ProfilePictureId: Guid.NewGuid().ToString()
    );
    
    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _store.DeleteProfile(_profile.Username);
    }

    public CosmosProfileStorageTest(WebApplicationFactory<Program> factory)
    {
        _store = factory.Services.GetRequiredService<IProfileStorage>();
    }
    
    [Fact]
    public async Task AddNewProfile()
    {
        await _store.UpsertProfile(_profile);
        Assert.Equal(_profile,  await _store.GetProfile(_profile.Username));
    }
    
    [Fact] 
    public async Task GetNonExistingProfile()
    {
        Assert.Null(await _store.GetProfile(_profile.Username));
    }

    [Fact]
    public async Task UpdateProfile()
    {
        await _store.UpsertProfile(_profile);
        var newProfile = new Profile(_profile.Username, "Bar", "Foo", "newId");
        await _store.UpsertProfile(newProfile);
        Assert.Equal(newProfile, await _store.GetProfile(_profile.Username));
    }

    [Fact]
    public async Task DeleteProfile()
    {
        await _store.UpsertProfile(_profile);
        Assert.Equal(_profile, await _store.GetProfile(_profile.Username));
        await _store.DeleteProfile(_profile.Username);
        Assert.Null(await _store.GetProfile(_profile.Username));
    }

    [Fact]
    public async Task DeleteNonExistingProfile()
    {
        await _store.DeleteProfile("non-existing");
    }
}
