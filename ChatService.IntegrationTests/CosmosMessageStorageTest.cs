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
        await _store.DeleteMessage(_message2.conversationId, _message2.messageId);
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

    [Xunit.Theory]
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

    [Fact]
    public async Task GetAlreadyExistingMessages()
    {
        Message expectedAlreadyExistingMessage1 = new Message("TestMessage", "My name is foo!", "foo", "foo_bar", 2000);
        Message expectedAlreadyExistingMessage1Flipped = new Message("TestMessage", "My name is foo!", "foo", "bar_foo", 2000);
        Assert.Equal(expectedAlreadyExistingMessage1, await _store.GetMessage("foo_bar", "TestMessage"));
        Assert.Equal(expectedAlreadyExistingMessage1Flipped, await _store.GetMessage("bar_foo", "TestMessage"));
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
        Assert.Null(await _store.GetMessage("mike_foo", _message1.messageId));
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

        List<Message> expectedMessagesForFoo = new List<Message>()
        {
            _message1,
            new Message(_message2.messageId, _message2.text, _message2.senderUsername, "foo_mike", _message2.unixTime)
        };
        
        List<Message> expectedMessagesForMike = new List<Message>()
        {
            new Message(_message1.messageId, _message1.text, _message1.senderUsername, "mike_foo", _message1.unixTime),
            _message2
        };

        var realMessagesForFoo = await _store.EnumerateMessagesFromAGivenConversation("foo_mike");
        var realMessagesForMike = await _store.EnumerateMessagesFromAGivenConversation("mike_foo");
        
        CollectionAssert.AreEquivalent(expectedMessagesForFoo, realMessagesForFoo); 
        CollectionAssert.AreEquivalent(expectedMessagesForMike, realMessagesForMike); 
    }
}