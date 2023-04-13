using System.Net;
using System.Text;
using ChatService.Dtos;
using ChatService.Storage;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;

namespace ChatService.Tests;


public class ConversationControllerTest :  IClassFixture<WebApplicationFactory<Program>>
{
    private readonly Mock<IConversationStorage> _conversationStorageMock = new();
    private readonly Mock<IMessageStorage> _messageStorageMock = new();
    private readonly HttpClient _httpClient;
    
    public ConversationControllerTest(WebApplicationFactory<Program> factory)
    {
        _httpClient = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services => { services.AddSingleton(_conversationStorageMock.Object); });
            builder.ConfigureTestServices(services => { services.AddSingleton(_messageStorageMock.Object); });
        }).CreateClient();
    }

    [Fact]
    public async Task SendValidMessageToConversation()
    {
        var sendMessageRequest = new SendMessageRequest(Guid.NewGuid().ToString(), "foobar", "Hello");
        var response = await _httpClient.PostAsync($"Conversation/foobar_mike/messages",
            new StringContent(JsonConvert.SerializeObject(sendMessageRequest), Encoding.Default, "application/json"));
        
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        _messageStorageMock.Verify(mock => mock.PostMessageToConversation(It.IsAny<Message>()), Times.Once);
    }

    [Theory]
    [InlineData(" ", "foobar", "Hello")]
    [InlineData("", "foobar", "Hello")]
    [InlineData("testId", "  ", "Hello")]
    [InlineData("testId", "foobar", "  ")]
    [InlineData("testId", "foobar", "")]
    [InlineData("testId", "", "Hello")]
    [InlineData(null, "foobar", "Hello")]
    [InlineData("testId", null, "Hello")]
    [InlineData("testId", "foobar", null)]
    public async Task SendMessageWithInvalidArgsToConversation(string messageId, string senderUsername, string text)
    {
        var message = new Message(messageId, text, senderUsername, "foo_bar", 10000);
        var response = await _httpClient.PostAsync("Conversation/foo_bar/messages",
            new StringContent(JsonConvert.SerializeObject(message), Encoding.Default, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        _messageStorageMock.Verify(mock => mock.PostMessageToConversation(message), Times.Never);
    }

    [Fact]
    public async Task EnumerateMessagesInAConversation()
    {
        List<Message> messages = new List<Message>()
        {
            new Message(Guid.NewGuid().ToString(), "Hi", "foo", "foo_bar", 1000),
            new Message(Guid.NewGuid().ToString(), "Hello", "bar", "bar_foo", 1001),
            new Message(Guid.NewGuid().ToString(), "What's up foo", "bar", "bar_foo", 1002)
        };

        _messageStorageMock.Setup(mock => mock.EnumerateMessagesFromAGivenConversation("foo_bar", null, null, null))
            .ReturnsAsync(new EnumerateMessagesStorageResponseDto(messages));
        var response = await _httpClient.GetAsync("Conversation/foo_bar/messages");

        Assert.Equal(HttpStatusCode.OK,response.StatusCode );
        var json = await response.Content.ReadAsStringAsync();
        Assert.Equal(messages, JsonConvert.DeserializeObject<EnumerateMessagesInAConversationResponseDto>(json).messages);
        _messageStorageMock.Verify(mock=>mock.EnumerateMessagesFromAGivenConversation("foo_bar", null, null, null), Times.Once);
    }

    [Fact]
    public async Task EnumerateMessagesInANotFoundConversation()
    {
        _messageStorageMock.Setup(m => m.EnumerateMessagesFromAGivenConversation("foo_bar", null, null, null))
            .ReturnsAsync((EnumerateMessagesStorageResponseDto?) null);
        var response = await _httpClient.GetAsync("Conversation/foo_bar/messages");
        Assert.Equal(HttpStatusCode.NotFound,response.StatusCode );
        _messageStorageMock.Verify(mock=>mock.EnumerateMessagesFromAGivenConversation("foo_bar", null, null, null), Times.Once);
    }

    [Fact]
    public async Task StartValidConversation()
    {
        var sendMessageRequest = new SendMessageRequest(Guid.NewGuid().ToString(), "foo", "Hello");
        var startConversationRequest = new StartConversationRequestDto("foo", "bar", sendMessageRequest);
        _conversationStorageMock.Setup(m => m.GetConversation("foo", "bar")).ReturnsAsync((Conversation?)null);
        
        var response = await _httpClient.PostAsync("Conversation/",
            new StringContent(JsonConvert.SerializeObject(startConversationRequest), Encoding.Default, "application/json"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        _conversationStorageMock.Verify(mock => mock.GetConversation("foo", "bar"), Times.Once);
        _conversationStorageMock.Verify(mock => mock.PostConversation(It.IsAny<Conversation>()), Times.Once);
        _messageStorageMock.Verify(mock => mock.PostMessageToConversation(It.IsAny<Message>()), Times.Once);
    }

    [Fact]
    public async Task StartAlreadyExistingConversation()
    {
        var sendMessageRequest = new SendMessageRequest(Guid.NewGuid().ToString(), "foo", "Hello");
        var startConversationRequest = new StartConversationRequestDto("foo", "bar", sendMessageRequest);
        _conversationStorageMock.Setup(m => m.GetConversation("foo", "bar"))
            .ReturnsAsync(new Conversation("foo", "bar", 1000));
        
        var response = await _httpClient.PostAsync("Conversation/",
            new StringContent(JsonConvert.SerializeObject(startConversationRequest), Encoding.Default, "application/json"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        _conversationStorageMock.Verify(mock => mock.GetConversation("foo", "bar"), Times.Once);
        _conversationStorageMock.Verify(mock => mock.PostConversation(It.IsAny<Conversation>()), Times.Never);
        _messageStorageMock.Verify(mock => mock.PostMessageToConversation(It.IsAny<Message>()), Times.Never);
    }

    [Theory]
    [InlineData("", "bar", "messageId", "foo", "hello")]
    [InlineData("  ", "bar", "messageId", "foo", "hello")]
    [InlineData(null, "bar", "messageId", "foo", "hello")]
    [InlineData("foo", "   ", "messageId", "foo", "hello")]
    [InlineData("foo", "bar", "", "foo", "hello")]
    [InlineData("foo", "bar", null, "foo", "hello")]
    [InlineData("foo", null, "messageId", "foo", "hello")]
    [InlineData("foo", "bar", "messageId", "foo", "  ")]
    [InlineData("foo", "bar", "messageId", "  ", "hello")]
    [InlineData("foo", "bar", "messageId", "", "hello")]
    [InlineData("foo", "bar", "  ", "foo", "hello")]
    [InlineData("foo", "bar", "messageId", null, "hello")]
    [InlineData("foo", "bar", "messageId", "foo", null)]
    public async Task StartConversationWithInvalidArgs(string userId1, string userId2, string messageId,
        string senderUsername, string text)
    {
        var sendMessageRequest = new SendMessageRequest(messageId, senderUsername, text);
        var startConversationRequest = new StartConversationRequestDto(userId1, userId2, sendMessageRequest);
        
        var response = await _httpClient.PostAsync("Conversation",
            new StringContent(JsonConvert.SerializeObject(startConversationRequest), Encoding.Default, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        _conversationStorageMock.Verify(mock => mock.PostConversation(It.IsAny<Conversation>()), Times.Never);
        _messageStorageMock.Verify(mock => mock.PostMessageToConversation(It.IsAny<Message>()), Times.Never);
    }

    [Fact]
    public async Task EnumerateConversationsForAUser()
    {
        List<Conversation> conversations = new List<Conversation>()
        {
            new Conversation( "foo", "bar", 1000),
            new Conversation( "foo", "mike", 1001),
            new Conversation( "foo", "john", 1002)
        };

        _conversationStorageMock.Setup(m => m.EnumerateConversationsForAGivenUser("foo", null, null, null))
            .ReturnsAsync(new EnumerateConversationsStorageResponseDto(conversations));
        var response = await _httpClient.GetAsync("Conversation?userId=foo");

        Assert.Equal(HttpStatusCode.OK,response.StatusCode );
        var json = await response.Content.ReadAsStringAsync();
        Assert.Equal(conversations, JsonConvert.DeserializeObject<EnumerateConversationsOfAGivenUserDto>(json).conversations);
        _conversationStorageMock.Verify(mock=>mock.EnumerateConversationsForAGivenUser("foo", null, null, null), Times.Once);
    }
    
    [Fact]
    public async Task EnumerateConversationsForANonExistingUser()
    {
        var response = await _httpClient.GetAsync("Conversation?userId=foo");
        _conversationStorageMock.Setup(m => m.EnumerateConversationsForAGivenUser("foo", null, null, null))
            .ReturnsAsync((EnumerateConversationsStorageResponseDto?) null);
        Assert.Equal(HttpStatusCode.NotFound,response.StatusCode );
        _conversationStorageMock.Verify(mock=>mock.EnumerateConversationsForAGivenUser("foo", null, null, null), Times.Once);
    }
}
