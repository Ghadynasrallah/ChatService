using ChatService.Dtos;
using ChatService.Storage;
using ChatService.Storage.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ChatService.Integrationtests;

public class CosmosMessageStorageTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly IMessageStorage _store;
    
    private readonly Message _message1 = new Message(Guid.NewGuid().ToString(), "Hello", "foo", "foo_mike", 10000);
    private readonly Message _message2 = new Message(Guid.NewGuid().ToString(), "Hi", "mike", "mike_foo", 10001);

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
        await _store.DeleteMessage(_message1.conversationId, _message1.messageId);
    }

    [Fact]
    public async Task PostValidMessage()
    {
        await _store.PostMessageToConversation(_message1);
        var _message1Flipped = new Message(_message1.messageId, _message1.text, _message1.senderUsername, "mike_foo",
            _message1.unixTime);
        
        Assert.Equal(_message1, await _store.GetMessage("foo_mike", _message1.messageId));
        Assert.Equal(_message1Flipped, await _store.GetMessage("mike_foo", _message1.messageId));
    }

    [Theory]
    [InlineData("", "hello", "foo", "message id", 1000)]
    [InlineData("  ", "hello", "foo", "message id", 1000)]
    [InlineData("foo_bar", null, "foo", "message id", 1000)]
    [InlineData("foo_bar", "hello", " ", "message id", 1000)]
    [InlineData("foo_bar", "hello", "foo", "   ", 1000)]
    [InlineData("foo_bar", "hello", "foo", null, 1000)]
    [InlineData("foo_bar", "", "foo", "message id", 1000)]
    public async Task PostInvalidMessage(string converstionId, string text, string senderUsername, string messageId, long unixTime)
    {
        var conversation = new Message(messageId, text, senderUsername, converstionId, unixTime);
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _store.PostMessageToConversation(conversation);
        });
    }
}