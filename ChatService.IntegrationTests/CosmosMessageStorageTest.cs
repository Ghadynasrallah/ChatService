using ChatService.Dtos;
using ChatService.Storage;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Assert = Xunit.Assert;

namespace ChatService.Integrationtests;

public class CosmosMessageStorageTest : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly IMessageStorage _store;
    
    private readonly Message _message1 = new Message(Guid.NewGuid().ToString(), "Hello", "foo", "foo_mike", 10000);
    private readonly Message _message2 = new Message(Guid.NewGuid().ToString(), "Hi", "mike", "foo_mike", 10010);
    private readonly Message _message3 = new Message(Guid.NewGuid().ToString(), "What's up", "foo", "foo_mike", 10020);


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
        await _store.DeleteMessage(_message1.ConversationId, _message1.MessageId);
        await _store.DeleteMessage(_message2.ConversationId, _message2.MessageId);
        await _store.DeleteMessage(_message3.ConversationId, _message3.MessageId);
    }

    [Fact]
    public async Task PostValidMessage()
    {
        await _store.PostMessageToConversation(_message1);
        Assert.Equal(_message1, await _store.GetMessage(_message1.ConversationId, _message1.MessageId));
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
        Assert.True(await _store.DeleteMessage(_message1.ConversationId, _message1.MessageId));
        Assert.Null(await _store.GetMessage(_message1.ConversationId, _message1.MessageId));
    }

    [Fact]
    public async Task DeleteNonExistingMessage()
    {
        Assert.False(await _store.DeleteMessage("mike_bar", "test"));
    }

    [Fact]
    public async Task EnumerateMessages()
    {
        await _store.PostMessageToConversation(_message1);
        await _store.PostMessageToConversation(_message2);
        await _store.PostMessageToConversation(_message3);

        List<Message> expectedMessages = new List<Message>()
        {
            _message3, _message2, _message1
        };

        var realMessages = await _store.EnumerateMessagesFromAGivenConversation("foo_mike");
        Assert.Equal(expectedMessages, realMessages?.Messages);
    }

    [Fact]
    public async Task EnumerateMessages_WithLimitAndContinuationToken()
    {
        await _store.PostMessageToConversation(_message1);
        await _store.PostMessageToConversation(_message2);
        await _store.PostMessageToConversation(_message3);

        List<Message> expectedMessages = new List<Message>()
        {
            _message3, _message2
        };

        var realMessages = await _store.EnumerateMessagesFromAGivenConversation("foo_mike", null, 2, null);
        Assert.Equal(expectedMessages, realMessages?.Messages);

        var secondResponse = await _store.EnumerateMessagesFromAGivenConversation("foo_mike", realMessages?.ContinuationToken, null, null);
        Assert.Equal(new List<Message>(){_message1}, secondResponse?.Messages);
    }

    [Fact]
    public async Task EnumerateMessages_WithLastSeenTime()
    {
        await _store.PostMessageToConversation(_message1);
        await _store.PostMessageToConversation(_message2);
        await _store.PostMessageToConversation(_message3);
        
        List<Message> expectedMessages = new List<Message>()
        {
            _message3, _message2
        };
        
        var realMessages = await _store.EnumerateMessagesFromAGivenConversation("foo_mike", null, null, 10005);
        Assert.Equal(expectedMessages, realMessages?.Messages);
    }

    [Fact]
    public async Task EnumerateMessages_NotFound()
    {
        var expectedResponse = new ListMessagesStorageResponseDto(new List<Message>());
        var actualResponse = await _store.EnumerateMessagesFromAGivenConversation("ghady_nasrallah");
        Assert.Equal(expectedResponse.Messages, actualResponse.Messages);
        Assert.Null(actualResponse.ContinuationToken);
    }
}