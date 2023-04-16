using ChatService.Dtos;
using ChatService.Exceptions;
using ChatService.Services;
using ChatService.Storage;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Moq;
using Microsoft.Extensions.DependencyInjection;

namespace ChatService.Tests;

public class ConversationServiceTest : IClassFixture<ConversationServiceTest>
{
    private readonly Mock<IConversationStorage> _conversationStorageMock = new();
    private readonly Mock<IMessageStorage> _messageStorageMock = new();
    private readonly Mock<IProfileStorage> _profileStorageMock = new();
    private readonly ConversationService _conversationService;

    public ConversationServiceTest()
    {
        _conversationService = new ConversationService(_conversationStorageMock.Object, _messageStorageMock.Object, _profileStorageMock.Object);
    }
    
    [Fact]
    public async Task SendMessageToConversation_Success()
    {
        // Arrange
        var sendMessageRequest = new SendMessageRequest(Guid.NewGuid().ToString(), "foo", "Hello");
        var conversation = new Conversation("bar_foo", "foo", "bar", 1000);

        _conversationStorageMock.Setup(x => x.GetConversation(conversation.ConversationId)).ReturnsAsync(conversation);
        _messageStorageMock.Setup(x => x.PostMessageToConversation(It.IsAny<Message>())).Returns(Task.CompletedTask);
        _conversationStorageMock.Setup(x => x.UpsertConversation(It.IsAny<PostConversationRequest>())).ReturnsAsync(conversation.ConversationId);

        // Act
        var result = await _conversationService.SendMessageToConversation(conversation.ConversationId, sendMessageRequest);

        // Assert
        Assert.Equal(sendMessageRequest.MessageId, result.messageId);
        Assert.Equal(sendMessageRequest.Text, result.text);
        Assert.Equal(sendMessageRequest.SenderUsername, result.senderUsername);
        Assert.Equal(conversation.ConversationId, result.conversationId);
        
        //Verify
        _conversationStorageMock.Verify(m=> m.UpsertConversation(new PostConversationRequest(conversation.UserId1, conversation.UserId2, result.unixTime)), Times.Once);
        _messageStorageMock.Verify(m=>m.PostMessageToConversation(result), Times.Once);
        _conversationStorageMock.Verify(m=>m.GetConversation(conversation.ConversationId), Times.Once);
    }

    [Theory]
    [InlineData(" ", "hello", "foo", "test")]
    [InlineData(null, "hello", "foo", "test")]
    [InlineData("bar_foo", " ", "foo", "test")]
    [InlineData("bar_foo", null, "foo", "test")]
    [InlineData("bar_foo", "hello", "  ", "test")]
    [InlineData("bar_foo", "hello", null, "test")]
    [InlineData("bar_foo", "hello", "foo", "  ")]
    [InlineData("bar_foo", "hello", "foo", null)]
    public async Task SendMessageToConversation_InvalidArgs(string conversationId, string text, string senderUsername, string messageId)
    {
        // Arrange
        var sendMessageRequest = new SendMessageRequest(messageId, senderUsername, text);
        await Assert.ThrowsAsync<ArgumentException>(()=> _conversationService.SendMessageToConversation(conversationId, sendMessageRequest));
    }

    [Fact]
    public async Task SendMessageToConversation_ConversationNotFound()
    {
        // Arrange
        var sendMessageRequest = new SendMessageRequest(Guid.NewGuid().ToString(), "foo", "Hello");
        var conversation = new Conversation("bar_foo", "foo", "bar", 1000);

        _conversationStorageMock.Setup(x => x.GetConversation(conversation.ConversationId)).ReturnsAsync((Conversation?)null);
        _messageStorageMock.Setup(x => x.PostMessageToConversation(It.IsAny<Message>())).Returns(Task.CompletedTask);
        _conversationStorageMock.Setup(x => x.UpsertConversation(It.IsAny<PostConversationRequest>())).ReturnsAsync(conversation.ConversationId);
        
        // Assert
        await Assert.ThrowsAsync<ConversationNotFoundException>(() =>
            _conversationService.SendMessageToConversation(conversation.ConversationId, sendMessageRequest));
        
        //Verify
        _conversationStorageMock.Verify(m=> m.UpsertConversation(It.IsAny<PostConversationRequest>()), Times.Never);
        _messageStorageMock.Verify(m=>m.PostMessageToConversation(It.IsAny<Message>()), Times.Never);
        _conversationStorageMock.Verify(m=>m.GetConversation(conversation.ConversationId), Times.Once);
    }
}