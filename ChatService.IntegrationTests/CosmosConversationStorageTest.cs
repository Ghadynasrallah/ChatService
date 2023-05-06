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

    private readonly Conversation _conversation1 = new Conversation("john_ripper", "john", "ripper", 100000);
    private readonly Conversation _conversation1Flipped = new Conversation("ripper_john", "ripper", "john", 100000);
    private readonly Conversation _conversation2 = new Conversation("mike_john", "mike", "john", 100200);
    private readonly Conversation _conversation3 = new Conversation("ripper_mike", "ripper", "mike", 100010);

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
        await _store.DeleteConversation(_conversation1.conversationId);
        await _store.DeleteConversation(_conversation2.conversationId);
        await _store.DeleteConversation(_conversation3.conversationId);
    }

    [Fact]
    public async Task PostValidConversation()
    {
        await _store.PostConversation(_conversation1);
        Assert.Equal(_conversation1, await _store.GetConversation(_conversation1.conversationId));
        Assert.Equal(_conversation1Flipped, await _store.GetConversation(_conversation1Flipped.conversationId));
    }

    [Xunit.Theory]
    [InlineData("foo_bar", "foo", "", 10000)]
    [InlineData("foo_bar", "   ", "bar", 10000)]
    [InlineData("", "foo", "bar", 10000)]
    [InlineData(null, "foo", "bar", 10000)]
    [InlineData("foo_bar", " ", "bar", 10000)]
    [InlineData("foo_bar", null, "bar", 10000)]
    public async Task PostInvalidConversation(string conversationId, string userId1, string userId2, long unixTime)
    {
        var conversation = new Conversation(conversationId, userId1, userId2, unixTime);
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _store.PostConversation(conversation);
        });
    }

    [Fact]
    public async Task GetAlreadyExistingConversation()
    {
        Assert.Equal(new Conversation("foo_bar", "foo", "bar", 10000), await _store.GetConversation("foo_bar"));
        Assert.Equal(new Conversation("bar_foo", "bar", "foo", 10000), await _store.GetConversation("bar_foo"));
    }

    [Fact]
    public async Task GetNonExistingConversation()
    {
        Assert.Null(await _store.GetConversation("mike_bar"));
    }

    [Fact]
    public async Task DeleteExistingConversation()
    {
        await _store.PostConversation(_conversation1);
        Assert.True(await _store.DeleteConversation(_conversation1.conversationId));
        Assert.Null(await _store.GetConversation(_conversation1.conversationId));
        Assert.Null(await _store.GetConversation(_conversation1Flipped.conversationId));
    }

    [Fact]
    public async Task DeleteNonExistingConversation()
    {
        Assert.False(await _store.DeleteConversation(_conversation1.conversationId));
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
            new Conversation("john_mike", "john", "mike", 100200)
        };
        
        List<Conversation> expectedConversationsForMike = new List<Conversation>()
        {
            _conversation2,
            new Conversation("mike_ripper", "mike", "ripper", 100010)
        };
        
        List<Conversation> expectedConversationsForRipper = new List<Conversation>()
        {
            _conversation3,
            new Conversation("ripper_john", "ripper", "john", 100000)
        };

        var realConversationsForJohn = await _store.EnumerateConversationsForAGivenUser("john");
        var realConversationsForMike = await _store.EnumerateConversationsForAGivenUser("mike");
        var realConversationsForRipper = await _store.EnumerateConversationsForAGivenUser("ripper");
        
        CollectionAssert.AreEquivalent(expectedConversationsForJohn, realConversationsForJohn); 
        CollectionAssert.AreEquivalent(expectedConversationsForMike, realConversationsForMike); 
        CollectionAssert.AreEquivalent(expectedConversationsForRipper, realConversationsForRipper); 
    }
}