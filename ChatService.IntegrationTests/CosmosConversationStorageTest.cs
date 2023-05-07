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

    private readonly PostConversationRequest _postConversation1 = new PostConversationRequest("john", "ripper", 100000);
    private readonly Conversation _conversation1 = new Conversation("john_ripper","john", "ripper", 100000);
    private readonly PostConversationRequest _postConversation2 = new PostConversationRequest("mike", "john", 100200);
    private readonly Conversation _conversation2 = new Conversation("john_mike","mike", "john", 100200);
    private readonly PostConversationRequest _postConversation3 = new PostConversationRequest("ripper", "mike", 100010);
    private readonly Conversation _conversation3 = new Conversation("mike_ripper","ripper", "mike", 100010);
    private readonly PostConversationRequest _postConversation4 = new PostConversationRequest("john", "foo", 100300);
    private readonly Conversation _conversation4 = new Conversation("foo_john", "john", "foo", 100300);
    
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
        await _store.DeleteConversation(_conversation1.UserId1, _conversation1.UserId2);
        await _store.DeleteConversation(_conversation2.UserId1, _conversation2.UserId2);
        await _store.DeleteConversation(_conversation3.UserId1, _conversation3.UserId2);
    }

    [Fact]
    public async Task PostValidConversation()
    {
        await _store.UpsertConversation(_postConversation1);
        Assert.Equal(_conversation1, await _store.GetConversation(_postConversation1.UserId1, _postConversation1.UserId2));
        Assert.Equal(_conversation1, await _store.GetConversation(_postConversation1.UserId2, _postConversation1.UserId1));
    }
    
    [Fact]
    public async Task GetConversation_WithConversationId()
    {
        await _store.UpsertConversation(_postConversation1);
        Assert.Equal(_conversation1, await _store.GetConversation(_conversation1.ConversationId));
    }

    [Fact]
    public async Task GetConversation_WithUserIds()
    {
        await _store.UpsertConversation(_postConversation2);
        Assert.Equal(_conversation2, await _store.GetConversation(_conversation2.UserId1, _conversation2.UserId2));
    }
    
    [Fact]
    public async Task GetConversation_NotFound()
    {
        Assert.Null(await _store.GetConversation("mike", "bar"));
    }

    [Fact]
    public async Task GetConversation_InvalidConversationId()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _store.GetConversation("foobar"));
    }

    [Fact]
    public async Task DeleteExistingConversation()
    {
        await _store.UpsertConversation(_postConversation1);
        Assert.True(await _store.DeleteConversation(_postConversation1.UserId1, _postConversation1.UserId2));
        Assert.Null(await _store.GetConversation(_postConversation1.UserId1, _postConversation1.UserId2));
        Assert.Null(await _store.GetConversation(_postConversation1.UserId2, _postConversation1.UserId1));
    }

    [Fact]
    public async Task DeleteNonExistingConversation()
    {
        Assert.False(await _store.DeleteConversation(_postConversation1.UserId1, _postConversation1.UserId2));
    }

    [Fact]
    public async Task EnumerateConversationsForAGivenUser()
    {
        await _store.UpsertConversation(_postConversation1);
        await _store.UpsertConversation(_postConversation2);
        await _store.UpsertConversation(_postConversation3);
        await _store.UpsertConversation(_postConversation4);

        List<Conversation> expectedConversationsForJohn = new List<Conversation>()
        {
            _conversation1,
            _conversation2, 
            _conversation4
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
        
        CollectionAssert.AreEquivalent(expectedConversationsForJohn, realConversationsForJohn.Conversations); 
        CollectionAssert.AreEquivalent(expectedConversationsForMike, realConversationsForMike.Conversations); 
        CollectionAssert.AreEquivalent(expectedConversationsForRipper, realConversationsForRipper.Conversations); 
    }
    
    [Fact]
    public async Task EnumerateConversations_WithLimitAndContinuationToken()
    {
        await _store.UpsertConversation(_postConversation4);
        await _store.UpsertConversation(_postConversation1);
        await _store.UpsertConversation(_postConversation2);

        List<Conversation> expectedConversationsForJohn = new List<Conversation>()
        {
            _conversation4,
            _conversation2
        };

        var realConversationsForJohn = await _store.EnumerateConversationsForAGivenUser("john", null, 2, null);
        Assert.Equal(expectedConversationsForJohn, realConversationsForJohn?.Conversations);
        
        var secondResponse = await _store.EnumerateConversationsForAGivenUser("john", realConversationsForJohn.ContinuationToken, null, null);
        Assert.Equal(new List<Conversation>{_conversation1}, secondResponse.Conversations);
    }
    
    [Fact]
    public async Task EnumerateConversations_WithLastSeenTime()
    {
        await _store.UpsertConversation(_postConversation1);
        await _store.UpsertConversation(_postConversation2);
        await _store.UpsertConversation(_postConversation4);

        List<Conversation> expectedConversationsForJohn = new List<Conversation>()
        {
            _conversation4
        };

        var realConversationsForJohn = await _store.EnumerateConversationsForAGivenUser("john", null, null, 100201);
        Assert.Equal(expectedConversationsForJohn, realConversationsForJohn?.Conversations);
    }

    [Fact]
    public async Task EnumerateConversations_NotFound()
    {
        var expectedResponse = new ListConversationsStorageResponse(new List<Conversation>());
        var actualResponse = await _store.EnumerateConversationsForAGivenUser("ghady");
        Assert.Equal(expectedResponse.Conversations, actualResponse.Conversations);
        Assert.Null(actualResponse.ContinuationToken);
    }
}