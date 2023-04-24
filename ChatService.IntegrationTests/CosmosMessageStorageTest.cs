using ChatService.Dtos;
using ChatService.Storage;
using ChatService.Storage.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Assert = Xunit.Assert;

namespace ChatService.Integrationtests;

public class CosmosMessageStorageTest : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly IMessageStorage _store;
    
    private readonly Message _message1 = new Message(Guid.NewGuid().ToString(), "Hello", "foo", "foo_mike", 10000);
    private readonly Message _message2 = new Message(Guid.NewGuid().ToString(), "Hi", "mike", "foo_mike", 10001);

    public CosmosMessageStorageTest(WebApplicationFactory<Program> factory)
    {
        _store = factory.Services.GetRequiredService<IMessageStorage>();
    }
    
    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _store.DeleteMessage(_message1.conversationId, _message1.messageId);
        await _store.DeleteMessage(_message2.conversationId, _message2.messageId);
    }

    [Fact]
    public async Task PostValidMessage()
    {
        await _store.PostMessageToConversation(_message1);
        Assert.Equal(_message1, await _store.GetMessage(_message1.conversationId, _message1.messageId));
    }
    
    [Fact]
    public async Task GetAlreadyExistingMessages()
    {
        Message expectedAlreadyExistingMessage = new Message("TestMessage", "My name is fooz!", "fooz", "barz_fooz", 2000);
        Assert.Equal(expectedAlreadyExistingMessage, await _store.GetMessage("barz_fooz", "TestMessage"));
    }

    [Fact]
    public async Task GetNonExistingMessage()
    {
        Assert.Null(await _store.GetMessage("mike_bar", "test"));
    }

    [Fact]
    public async Task DeleteExistingMessage()
    {
        await _store.PostMessageToConversation(_message1);
        Assert.True(await _store.DeleteMessage(_message1.conversationId, _message1.messageId));
        Assert.Null(await _store.GetMessage(_message1.conversationId, _message1.messageId));
    }

    [Fact]
    public async Task DeleteNonExistingMessage()
    {
        Assert.False(await _store.DeleteMessage("mike_bar", "test"));
    }

    [Fact]
    public async Task EnumerateMessagesFromAGivenConversation()
    {
        await _store.PostMessageToConversation(_message1);
        await _store.PostMessageToConversation(_message2);

        List<Message> expectedMessages = new List<Message>()
        {
            _message1, _message2
        };

        var realMessages = await _store.EnumerateMessagesFromAGivenConversation("foo_mike");
        CollectionAssert.AreEquivalent(expectedMessages, realMessages?.Messages);
    }
}