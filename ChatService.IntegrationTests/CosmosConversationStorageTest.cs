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
    public async Task GetAlreadyExistingConversation()
    {
        Assert.Equal(new Conversation("barz_fooz","fooz", "barz", 1000), await _store.GetConversation("fooz", "barz"));
    }

    [Fact]
    public async Task GetNonExistingConversation()
    {
        Assert.Null(await _store.GetConversation("mike", "bar"));
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
        
        CollectionAssert.AreEquivalent(expectedConversationsForJohn, realConversationsForJohn.Conversations); 
        CollectionAssert.AreEquivalent(expectedConversationsForMike, realConversationsForMike.Conversations); 
        CollectionAssert.AreEquivalent(expectedConversationsForRipper, realConversationsForRipper.Conversations); 
    }
}