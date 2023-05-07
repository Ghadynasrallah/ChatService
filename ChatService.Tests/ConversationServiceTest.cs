using ChatService.Dtos;
using ChatService.Exceptions;
using ChatService.Services;
using ChatService.Storage;
using Microsoft.AspNetCore.Mvc.Testing;
using Moq;

namespace ChatService.Tests;

public class ConversationServiceTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly Mock<IConversationStorage> _conversationStorageMock = new();
    private readonly Mock<IMessageStorage> _messageStorageMock = new();
    private readonly Mock<IProfileStorage> _profileStorageMock = new();
    private readonly ConversationService _conversationService;

    public ConversationServiceTest()
    {
        _conversationService = new ConversationService(_conversationStorageMock.Object, _messageStorageMock.Object, _profileStorageMock.Object);
    }

    private readonly Conversation _conversation1 = new Conversation("bar_foo", "foo", "bar", 1000);
    
    [Fact]
    public async Task SendMessageToConversation_Success()
    {
        // Arrange
        var sendMessageRequest = new SendMessageRequest(Guid.NewGuid().ToString(), "foo", "Hello");

        _conversationStorageMock.Setup(x => x.GetConversation(_conversation1.ConversationId)).ReturnsAsync(_conversation1);
        _messageStorageMock.Setup(x => x.PostMessageToConversation(It.IsAny<Message>())).Returns(Task.CompletedTask);
        _conversationStorageMock.Setup(x => x.UpsertConversation(It.IsAny<PostConversationRequest>())).ReturnsAsync(_conversation1.ConversationId);

        // Act
        var result = await _conversationService.SendMessageToConversation(_conversation1.ConversationId, sendMessageRequest);

        // Assert
        Assert.Equal(sendMessageRequest.Id, result.MessageId);
        Assert.Equal(sendMessageRequest.Text, result.Text);
        Assert.Equal(sendMessageRequest.SenderUsername, result.SenderUsername);
        Assert.Equal(_conversation1.ConversationId, result.ConversationId);
        
        //Verify
        _conversationStorageMock.Verify(m=> m.UpsertConversation(new PostConversationRequest(_conversation1.UserId1, _conversation1.UserId2, result.UnixTime)), Times.Once);
        _messageStorageMock.Verify(m=>m.PostMessageToConversation(result), Times.Once);
        _conversationStorageMock.Verify(m=>m.GetConversation(_conversation1.ConversationId), Times.Once);
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
        _conversationStorageMock.Setup(m => m.GetConversation("bar_foo"))
            .ReturnsAsync(new Conversation("bar_foo", "foo", "bar", 100));
        await Assert.ThrowsAsync<ArgumentException>(()=> _conversationService.SendMessageToConversation(conversationId, sendMessageRequest));
    }

    [Fact]
    public async Task SendMessageToConversation_ConversationNotFound()
    {
        // Arrange
        var sendMessageRequest = new SendMessageRequest(Guid.NewGuid().ToString(), "foo", "Hello");

        _conversationStorageMock.Setup(x => x.GetConversation(_conversation1.ConversationId)).ReturnsAsync((Conversation?)null);
        _messageStorageMock.Setup(x => x.PostMessageToConversation(It.IsAny<Message>())).Returns(Task.CompletedTask);
        _conversationStorageMock.Setup(x => x.UpsertConversation(It.IsAny<PostConversationRequest>())).ReturnsAsync(_conversation1.ConversationId);
        
        // Assert
        await Assert.ThrowsAsync<ConversationNotFoundException>(() =>
            _conversationService.SendMessageToConversation(_conversation1.ConversationId, sendMessageRequest));
        
        //Verify
        _conversationStorageMock.Verify(m=> m.UpsertConversation(It.IsAny<PostConversationRequest>()), Times.Never);
        _messageStorageMock.Verify(m=>m.PostMessageToConversation(It.IsAny<Message>()), Times.Never);
        _conversationStorageMock.Verify(m=>m.GetConversation(_conversation1.ConversationId), Times.Once);
    }
    
    [Fact]
    public async Task SendMessageToConversation_SenderNotParticipant()
    {
        // Arrange
        var sendMessageRequest = new SendMessageRequest(Guid.NewGuid().ToString(), "NotParticipant", "Hello");

        _conversationStorageMock.Setup(x => x.GetConversation(_conversation1.ConversationId)).ReturnsAsync(_conversation1);
        _messageStorageMock.Setup(x => x.PostMessageToConversation(It.IsAny<Message>())).Returns(Task.CompletedTask);
        _conversationStorageMock.Setup(x => x.UpsertConversation(It.IsAny<PostConversationRequest>())).ReturnsAsync(_conversation1.ConversationId);
        
        // Assert
        await Assert.ThrowsAsync<SenderNotParticipantException>(() =>
            _conversationService.SendMessageToConversation(_conversation1.ConversationId, sendMessageRequest));
        
        //Verify
        _conversationStorageMock.Verify(m=> m.UpsertConversation(It.IsAny<PostConversationRequest>()), Times.Never);
        _messageStorageMock.Verify(m=>m.PostMessageToConversation(It.IsAny<Message>()), Times.Never);
        _conversationStorageMock.Verify(m=>m.GetConversation(_conversation1.ConversationId), Times.Once);
    }
    
    [Fact]
    public async Task SendMessageToConversation_MessageConflict()
    {
        // Arrange
        var sendMessageRequest = new SendMessageRequest(Guid.NewGuid().ToString(), "foo", "Hello");
        var message = new Message(sendMessageRequest.Id, sendMessageRequest.Text, sendMessageRequest.SenderUsername,
            _conversation1.ConversationId, 100);

            _conversationStorageMock.Setup(x => x.GetConversation(_conversation1.ConversationId)).ReturnsAsync(_conversation1);
        _messageStorageMock.Setup(x => x.PostMessageToConversation(It.IsAny<Message>())).Returns(Task.CompletedTask);
        _messageStorageMock.Setup(x => x.GetMessage(_conversation1.ConversationId, sendMessageRequest.Id))
            .ReturnsAsync(message);
        _conversationStorageMock.Setup(x => x.UpsertConversation(It.IsAny<PostConversationRequest>())).ReturnsAsync(_conversation1.ConversationId);
        
        // Assert
        await Assert.ThrowsAsync<MessageConflictException>(() =>
            _conversationService.SendMessageToConversation(_conversation1.ConversationId, sendMessageRequest));
        
        //Verify
        _conversationStorageMock.Verify(m=> m.UpsertConversation(It.IsAny<PostConversationRequest>()), Times.Never);
        _messageStorageMock.Verify(m=>m.PostMessageToConversation(It.IsAny<Message>()), Times.Never);
        _messageStorageMock.Verify(m=>m.GetMessage(_conversation1.ConversationId, sendMessageRequest.Id), Times.Once);
        _conversationStorageMock.Verify(m=>m.GetConversation(_conversation1.ConversationId), Times.Once);
    }

    [Fact]
    public async Task EnumerateMessages_Success()
    {
        //Arrange
        List<Message> messages = new List<Message>()
        {
            new Message(Guid.NewGuid().ToString(), "Hey bar", _conversation1.UserId1,
            _conversation1.ConversationId, 1000),
            
            new Message(Guid.NewGuid().ToString(), "Hey foo", _conversation1.UserId2,
                _conversation1.ConversationId, 1001), 
            
            new Message(Guid.NewGuid().ToString(), "What's up", _conversation1.UserId1,
                _conversation1.ConversationId, 1002)
        };

        List<ListMessageResponseItem> messageResponseItems = new List<ListMessageResponseItem>
        {
            new ListMessageResponseItem(messages[0].Text, messages[0].SenderUsername, messages[0].UnixTime),
            new ListMessageResponseItem(messages[1].Text, messages[1].SenderUsername, messages[1].UnixTime),
            new ListMessageResponseItem(messages[2].Text, messages[2].SenderUsername, messages[2].UnixTime),
        };
            
        var messagesStorageResponse = new ListMessagesStorageResponseDto(messages);
        var messagesServiceResponse = new ListMessageServiceResponseDto(messageResponseItems);

        _messageStorageMock.Setup(x => x.EnumerateMessagesFromAGivenConversation(_conversation1.ConversationId, null, null, null))
            .ReturnsAsync(messagesStorageResponse);
        _conversationStorageMock.Setup(x => x.GetConversation(_conversation1.ConversationId))
            .ReturnsAsync(_conversation1);
        
        //Assert
        var conversationServiceResponse =
            await _conversationService.EnumerateMessagesInAConversation(_conversation1.ConversationId);
        Assert.Equal(conversationServiceResponse.Messages, messagesServiceResponse.Messages);
        Assert.Equal(conversationServiceResponse.ContinuationToken, messagesServiceResponse.ContinuationToken);
        
        //Verify
        _messageStorageMock.Verify(m=> m.EnumerateMessagesFromAGivenConversation(_conversation1.ConversationId, null, null, null), Times.Once);
    }

    [Theory]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("")]
    public async Task EnumerateMessages_InvalidArg(string conversationId)
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _conversationService.EnumerateMessagesInAConversation(conversationId));
    }

    [Fact]
    public async Task EnumerateMessages_ConversationDoesNotExist()
    {
        _conversationStorageMock.Setup(m => m.GetConversation(_conversation1.ConversationId))
            .ReturnsAsync((Conversation?)null);
        await Assert.ThrowsAsync<ConversationNotFoundException>(() =>
            _conversationService.EnumerateMessagesInAConversation(_conversation1.ConversationId));
    }

    [Fact]
    public async Task EnumerateMessages_NoMessages()
    {
        _conversationStorageMock.Setup(m => m.GetConversation(_conversation1.ConversationId))
            .ReturnsAsync(_conversation1);
        _messageStorageMock.Setup(m=>m.EnumerateMessagesFromAGivenConversation(_conversation1.ConversationId, null, null, null))
            .ReturnsAsync(new ListMessagesStorageResponseDto(new List<Message>()));
        var expectedResult = new ListMessageServiceResponseDto(new List<ListMessageResponseItem>());
        var actualResult = await _conversationService.EnumerateMessagesInAConversation(_conversation1.ConversationId);
        Assert.Equal(expectedResult.ContinuationToken, actualResult.ContinuationToken);
        Assert.Equal(expectedResult.Messages, actualResult.Messages);
    }
    
    [Fact]
    public async Task StartConversation_Success()
    {
        //Arrange
        var sendMessageRequest = new SendMessageRequest(Guid.NewGuid().ToString(), _conversation1.UserId1, "hello");
        var startConversationRequest = new AddConversationRequest(new []{_conversation1.UserId1, _conversation1.UserId2}, sendMessageRequest);
        var profile1 = new Profile("foo", "foo", "foo", null);
        var profile2 = new Profile("bar", "nar", "bar", null);

        _profileStorageMock.Setup(m => m.GetProfile("foo")).ReturnsAsync(profile1);
        _profileStorageMock.Setup(m => m.GetProfile("bar")).ReturnsAsync(profile2);
        _conversationStorageMock.Setup(m => m.GetConversation(_conversation1.UserId1, _conversation1.UserId2))
            .ReturnsAsync((Conversation?)null);
        _conversationStorageMock.Setup(m=>m.UpsertConversation(It.IsAny<PostConversationRequest>())).ReturnsAsync("bar_foo");

        var startConversationResponse = new AddConversationResponse(_conversation1.ConversationId,
            new [] { _conversation1.UserId1, _conversation1.UserId2 }, It.IsAny<DateTime>());
        var actualResult = await _conversationService.StartConversation(startConversationRequest);
        
        //Assert
        Assert.Equal(actualResult.Id, startConversationResponse.Id);
        Assert.Equal(actualResult.Participants, startConversationResponse.Participants);
        
        //Verify
        _messageStorageMock.Verify(m=> m.PostMessageToConversation(It.IsAny<Message>()), Times.Once);
        _conversationStorageMock.Verify(m=>m.UpsertConversation(It.IsAny<PostConversationRequest>()), Times.Once);
        _conversationStorageMock.Verify(m=>m.GetConversation(_conversation1.UserId1, _conversation1.UserId2), Times.Once);
    }

    [Theory]
    [InlineData("  ", "foo", "hello", "foo", "bar")]
    [InlineData(null, "foo", "hello", "foo", "bar")]
    [InlineData("test", "", "hello", "foo", "bar")]
    [InlineData("test", "  ", "hello", "foo", "bar")]
    [InlineData("test", null, "hello", "foo", "bar")]
    [InlineData("test", "foo", "  ", "foo", "bar")]
    [InlineData("test", "foo", "", "foo", "bar")]
    [InlineData("test", "foo", null, "foo", "bar")]
    [InlineData("test", "foo", "hello", "", "bar")]
    [InlineData("test", "foo", "hello", "  ", "bar")]
    [InlineData("test", "foo", "hello", null, "bar")]
    [InlineData("test", "foo", "hello", "foo", "")]
    [InlineData("test", "foo", "hello", "foo", "  ")]
    [InlineData("test", "foo", "hello", "foo", null)]
    public async Task StartConversation_InvalidArgs(string messageId, string senderUsername, string text, string userId1, string userId2) 
    {
        var sendMessageRequest = new SendMessageRequest(messageId, senderUsername, text);
        var startConversationRequest = new AddConversationRequest(new []{userId1, userId2}, sendMessageRequest);
        _profileStorageMock.Setup(m => m.GetProfile("foo"))
            .ReturnsAsync(new Profile("foo", "foo", "bar", null));
        _profileStorageMock.Setup(m => m.GetProfile("bar"))
            .ReturnsAsync(new Profile("bar", "bar", "foo", null));
        await Assert.ThrowsAsync<ArgumentException>(() => _conversationService.StartConversation(startConversationRequest));
    }

    [Fact]
    public async Task StartConversation_InvalidNumberOfUsers()
    {
        var sendMessageRequest = new SendMessageRequest(Guid.NewGuid().ToString(), _conversation1.UserId1, "Hello");
        string[] participants = new string[] { "foo", "bar", "mike" };
        var startConversationRequest = new AddConversationRequest(participants, sendMessageRequest);
        await Assert.ThrowsAsync<ArgumentException>(() => _conversationService.StartConversation(startConversationRequest));
    }

    [Fact]
    public async Task StartConversation_UserNotFound()
    {
        //Arrange
        var sendMessageRequest = new SendMessageRequest(Guid.NewGuid().ToString(), _conversation1.UserId1, "Hello");
        var startConversationRequest =
            new AddConversationRequest(new [] { _conversation1.UserId1, _conversation1.UserId2 }, sendMessageRequest);
        var profile1 = new Profile(_conversation1.UserId1, _conversation1.UserId1, _conversation1.UserId1, null);
        var profile2 = new Profile(_conversation1.UserId2, _conversation1.UserId2, _conversation1.UserId2, null);

        _profileStorageMock.Setup(m => m.GetProfile(profile1.Username)).ReturnsAsync((Profile?)null);
        _profileStorageMock.Setup(m => m.GetProfile(profile2.Username)).ReturnsAsync((Profile?)null);
        _conversationStorageMock.Setup(m => m.GetConversation(_conversation1.UserId1, _conversation1.UserId2))
            .ReturnsAsync((Conversation?)null);
        _conversationStorageMock.Setup(m=>m.UpsertConversation(It.IsAny<PostConversationRequest>())).ReturnsAsync("bar_foo");
        
        //Assert
        await Assert.ThrowsAsync<UserNotFoundException>(() => _conversationService.StartConversation(startConversationRequest));
    }
    
    [Fact]
    public async Task StartConversation_SenderNotParticipant()
    {
        //Arrange
        var sendMessageRequest = new SendMessageRequest(Guid.NewGuid().ToString(), "NotParticipant", "Hello");
        var startConversationRequest =
            new AddConversationRequest(new [] { _conversation1.UserId1, _conversation1.UserId2 }, sendMessageRequest);
        var profile1 = new Profile(_conversation1.UserId1, _conversation1.UserId1, _conversation1.UserId1, null);
        var profile2 = new Profile(_conversation1.UserId2, _conversation1.UserId2, _conversation1.UserId2, null);

        _profileStorageMock.Setup(m => m.GetProfile(profile1.Username)).ReturnsAsync(profile1);
        _profileStorageMock.Setup(m => m.GetProfile(profile2.Username)).ReturnsAsync(profile2);
        _conversationStorageMock.Setup(m => m.GetConversation(_conversation1.UserId1, _conversation1.UserId2))
            .ReturnsAsync((Conversation?)null);
        _conversationStorageMock.Setup(m=>m.UpsertConversation(It.IsAny<PostConversationRequest>())).ReturnsAsync("bar_foo");
        
        //Assert
        await Assert.ThrowsAsync<SenderNotParticipantException>(() => _conversationService.StartConversation(startConversationRequest));
    }

    [Fact]
    public async Task StartConversation_ConversationAlreadyExists()
    {
        //Arrange
        var sendMessageRequest = new SendMessageRequest(Guid.NewGuid().ToString(), _conversation1.UserId1, "Hello");
        var startConversationRequest = new AddConversationRequest(new [] { _conversation1.UserId1, _conversation1.UserId2 }, sendMessageRequest);
        var profile1 = new Profile(_conversation1.UserId1, "foo", "foo", null);
        var profile2 = new Profile(_conversation1.UserId2, "nar", "bar", null);

        _profileStorageMock.Setup(m => m.GetProfile(_conversation1.UserId1)).ReturnsAsync(profile1);
        _profileStorageMock.Setup(m => m.GetProfile(_conversation1.UserId2)).ReturnsAsync(profile2);
        _conversationStorageMock.Setup(m => m.GetConversation(profile1.Username, profile2.Username))
            .ReturnsAsync(_conversation1);
        
        //Assert
        await Assert.ThrowsAsync<ConversationConflictException>(() =>
            _conversationService.StartConversation(startConversationRequest));
        
        //Verify
        _conversationStorageMock.Verify(m=>m.GetConversation(profile1.Username, profile2.Username), Times.Once);
    }
    [Fact]
    public async Task EnumerateConversations()
    {
        //Arrange
        var profile1 = new Profile("foo", "foo", "foo", null);
        var profile2 = new Profile("mike", "mike", "mike", null);
        var profile3 = new Profile("bar", "bar", "bar", null);
        var conversation2 = new Conversation("foo_mike", "foo", "mike", 100);
        List<Conversation> conversations = new List<Conversation>()
        {
            _conversation1, conversation2
        };

        List<ListConversationsResponseItem> conversationsResponseItems = new List<ListConversationsResponseItem>()
        {
            new ListConversationsResponseItem(_conversation1.ConversationId, _conversation1.LastModifiedUnixTime, profile3),
            new ListConversationsResponseItem(conversation2.ConversationId, conversation2.LastModifiedUnixTime, profile2)
        };

        _conversationStorageMock.Setup(m => m.EnumerateConversationsForAGivenUser(profile1.Username, null, null, null))
            .ReturnsAsync(new ListConversationsStorageResponse(conversations));
        _profileStorageMock.Setup(m => m.GetProfile(profile1.Username)).ReturnsAsync(profile1);
        _profileStorageMock.Setup(m => m.GetProfile(profile2.Username)).ReturnsAsync(profile2);
        _profileStorageMock.Setup(m => m.GetProfile(profile3.Username)).ReturnsAsync(profile3);
        
        //Assert
        var response = await _conversationService.EnumerateConversationsOfAGivenUser(profile1.Username);
        Assert.Equal(response.Conversations, conversationsResponseItems);
    }

    [Theory]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("")]
    public async Task EnumerateConversations_InvalidArgs(string userId)
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _conversationService.EnumerateConversationsOfAGivenUser(userId));
    }

    [Fact]
    public async Task EnumerateConversations_UserDoesNotExist()
    {
        var profile1 = new Profile(_conversation1.UserId1, _conversation1.UserId1, _conversation1.UserId1, null);
        _profileStorageMock.Setup(m => m.GetProfile(profile1.Username)).ReturnsAsync((Profile?)null);
        await Assert.ThrowsAsync<UserNotFoundException>(() =>
            _conversationService.EnumerateConversationsOfAGivenUser(profile1.Username));
    }

    [Fact]
    public async Task EnumerateConversations_NoConversationsExist()
    {
        var profile1 = new Profile(_conversation1.UserId1, _conversation1.UserId1, _conversation1.UserId1, null);
        _profileStorageMock.Setup(m => m.GetProfile(profile1.Username)).ReturnsAsync(profile1);
        _conversationStorageMock.Setup(m => m.EnumerateConversationsForAGivenUser(profile1.Username, null, null, null))
            .ReturnsAsync(new ListConversationsStorageResponse(new List<Conversation>()));
        var expectedResult = new ListConversationsServiceResponse(new List<ListConversationsResponseItem>());
        var actualResult = await _conversationService.EnumerateConversationsOfAGivenUser(profile1.Username);
        Assert.Equal(expectedResult.Conversations, actualResult.Conversations);
        Assert.Equal(expectedResult.ContinuationToken, actualResult.ContinuationToken);
    }
}