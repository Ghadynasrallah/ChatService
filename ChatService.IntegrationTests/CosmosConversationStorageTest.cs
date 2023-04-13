using ChatService.Dtos;
using ChatService.Storage;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Assert = Xunit.Assert;


namespace ChatService.Integrationtests;

public class CosmosConversationStorageTest :  IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly IConversationStorage _store;

    private readonly Conversation _conversation1 = new Conversation("john", "ripper", 100000);
    private readonly Conversation _conversation2 = new Conversation("mike", "john", 100200);
    private readonly Conversation _conversation3 = new Conversation("ripper", "mike", 100010);

    public CosmosConversationStorageTest(WebApplicationFactory<Program> factory)
    {
        _store = factory.Services.GetRequiredService<IConversationStorage>();
    }
    
    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _store.DeleteConversation(_conversation1.userId1, _conversation1.userId2);
        await _store.DeleteConversation(_conversation2.userId1, _conversation2.userId2);
        await _store.DeleteConversation(_conversation3.userId1, _conversation3.userId2);
    }

    [Fact]
    public async Task PostValidConversation()
    {
        await _store.PostConversation(_conversation1);
        Assert.Equal(_conversation1, await _store.GetConversation(_conversation1.userId1, _conversation1.userId2));
        Assert.Equal(_conversation1, await _store.GetConversation(_conversation1.userId2, _conversation1.userId1));
    }

    [Xunit.Theory]
    [InlineData("foo", "", 10000)]
    [InlineData("   ", "bar", 10000)]
    [InlineData("foo", null, 10000)]
    [InlineData("", "bar", 10000)]
    [InlineData(null, "bar", 10000)]
    public async Task PostInvalidConversation(string userId1, string userId2, long unixTime)
    {
        var conversation = new Conversation(userId1, userId2, unixTime);
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _store.PostConversation(conversation);
        });
    }

    [Fact]
    public async Task GetAlreadyExistingConversation()
    {
        Assert.Equal(new Conversation("fooz", "barz", 1000), await _store.GetConversation("fooz", "barz"));
    }

    [Fact]
    public async Task GetNonExistingConversation()
    {
        Assert.Null(await _store.GetConversation("mike", "bar"));
    }

    [Fact]
    public async Task DeleteExistingConversation()
    {
        await _store.PostConversation(_conversation1);
        Assert.True(await _store.DeleteConversation(_conversation1.userId1, _conversation1.userId2));
        Assert.Null(await _store.GetConversation(_conversation1.userId1, _conversation2.userId2));
        Assert.Null(await _store.GetConversation(_conversation1.userId2, _conversation2.userId1));
    }

    [Fact]
    public async Task DeleteNonExistingConversation()
    {
        Assert.False(await _store.DeleteConversation(_conversation1.userId1, _conversation2.userId2));
    }

    [Fact]
    public async Task EnumerateConversationsForAGivenUser()
    {
        await _store.PostConversation(_conversation1);
        await _store.PostConversation(_conversation2);
        await _store.PostConversation(_conversation3);

        List<Conversation> expectedConversationsForJohn = new List<Conversation>()
        {
            _conversation1,
            _conversation2
        };
        
        List<Conversation> expectedConversationsForMike = new List<Conversation>()
        {
            _conversation2,
            _conversation3
        };
        
        List<Conversation> expectedConversationsForRipper = new List<Conversation>()
        {
            _conversation1,
            _conversation3
        };

        var realConversationsForJohn = await _store.EnumerateConversationsForAGivenUser("john");
        var realConversationsForMike = await _store.EnumerateConversationsForAGivenUser("mike");
        var realConversationsForRipper = await _store.EnumerateConversationsForAGivenUser("ripper");
        
        CollectionAssert.AreEquivalent(expectedConversationsForJohn, realConversationsForJohn.conversations); 
        CollectionAssert.AreEquivalent(expectedConversationsForMike, realConversationsForMike.conversations); 
        CollectionAssert.AreEquivalent(expectedConversationsForRipper, realConversationsForRipper.conversations); 
    }
}