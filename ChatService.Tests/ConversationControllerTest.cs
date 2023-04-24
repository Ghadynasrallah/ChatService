using System.Net;
using System.Text;
using System.Web;
using ChatService.Dtos;
using ChatService.Exceptions;
using ChatService.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;

namespace ChatService.Tests;

public class ConversationControllerTest :  IClassFixture<WebApplicationFactory<Program>>
{
    private readonly Mock<IConversationService> _conversationServiceMock = new();
    private readonly HttpClient _httpClient;
    private readonly Conversation _conversation1 = new Conversation("bar_foo", "foo", "bar", 1000);
    private readonly Message _message1 = new Message(Guid.NewGuid().ToString(), "hello", "foo", "bar_foo", 1000);
    
    public ConversationControllerTest(WebApplicationFactory<Program> factory)
    {
        _httpClient = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services => { services.AddSingleton(_conversationServiceMock.Object); });
        }).CreateClient();
    }

    [Fact]
    public async Task SendMessage_Success()
    {
        var sendMessageRequest = new SendMessageRequest(_message1.messageId, _message1.senderUsername, _message1.text);
        _conversationServiceMock.Setup(m =>
            m.SendMessageToConversation(_conversation1.ConversationId, sendMessageRequest)).ReturnsAsync(_message1);
        
        var response = await _httpClient.PostAsync($"Conversation/bar_foo/messages",
            new StringContent(JsonConvert.SerializeObject(sendMessageRequest), Encoding.Default, "application/json"));
        var json = await response.Content.ReadAsStringAsync();
        var sendMessageResponse = JsonConvert.DeserializeObject<SendMessageResponse>(json);
        
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(_message1.unixTime, sendMessageResponse.CreatedUnixTime);
        _conversationServiceMock.Verify(mock => mock.SendMessageToConversation(_conversation1.ConversationId, sendMessageRequest), Times.Once);
    }
    
    [Fact]
    public async Task SendMessage_BadRequest()
    {
        //Setup
        var sendMessageRequest = new SendMessageRequest(_message1.messageId, _message1.senderUsername, _message1.text);
        _conversationServiceMock.Setup(m => m.SendMessageToConversation(_conversation1.ConversationId, sendMessageRequest))
            .ThrowsAsync(new ArgumentException());
        
        //Act
        var response = await _httpClient.PostAsync($"Conversation/{_conversation1.ConversationId}/messages",
            new StringContent(JsonConvert.SerializeObject(sendMessageRequest), Encoding.Default, "application/json"));
        
        //Assert and Verify
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        _conversationServiceMock.Verify(mock => mock.SendMessageToConversation(_conversation1.ConversationId, sendMessageRequest), Times.Once);
    }
    
    [Fact]
    public async Task EnumerateMessagesInAConversation_Success()
    {
        List<ListMessageResponseItem> messages = new List<ListMessageResponseItem>()
        {
            new ListMessageResponseItem(_message1.text, _message1.senderUsername, _message1.unixTime),
            new ListMessageResponseItem("Hello", "bar", 1001)
        };
        var encodedToken = HttpUtility.UrlEncode("test/token");
        var encodedResponseToken = HttpUtility.UrlEncode("token/response");
        var nextUri = $"Conversation/{_conversation1.ConversationId}/messages?lastSeenMessageTime=1001&limit=2&continuationToken={encodedResponseToken}";
        
        _conversationServiceMock.Setup(mock => mock.EnumerateMessagesInAConversation("bar_foo", "test/token", 2, 1001))
            .ReturnsAsync(new ListMessageServiceResponseDto(messages, "token/response"));
        var response = await _httpClient.GetAsync($"Conversation/{_conversation1.ConversationId}/messages?lastSeenMessageTime=1001&limit=2&continuationToken={encodedToken}");

        Assert.Equal(HttpStatusCode.OK,response.StatusCode );
        var json = await response.Content.ReadAsStringAsync();
        var listMessageResponse = JsonConvert.DeserializeObject<ListMessageResponse>(json);
        Assert.Equal(messages, listMessageResponse.Messages);
        Assert.Equal(nextUri, listMessageResponse.NextUri);
        _conversationServiceMock.Verify(mock=>mock.EnumerateMessagesInAConversation("bar_foo", "test/token", 2, 1001), Times.Once);
    }
    
    [Fact]
    public async Task EnumerateMessages_ConversationNotFound()
    {
        _conversationServiceMock.Setup(m => m.EnumerateMessagesInAConversation(_conversation1.ConversationId, null, null, null))
            .ThrowsAsync(new ConversationNotFoundException());
        var response = await _httpClient.GetAsync($"Conversation/{_conversation1.ConversationId}/messages");
        Assert.Equal(HttpStatusCode.NotFound,response.StatusCode );
        _conversationServiceMock.Verify(mock=>mock.EnumerateMessagesInAConversation(_conversation1.ConversationId, null, null, null), Times.Once);
    }

    [Fact]
    public async Task EnumerateMessages_InvalidArgs()
    {
        _conversationServiceMock.Setup(m => m.EnumerateMessagesInAConversation(_conversation1.ConversationId, null, null, null))
            .ThrowsAsync(new ArgumentException());
        var response = await _httpClient.GetAsync($"Conversation/{_conversation1.ConversationId}/messages");
        Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode );
        _conversationServiceMock.Verify(mock=>mock.EnumerateMessagesInAConversation(_conversation1.ConversationId, null, null, null), Times.Once);
    }

    [Fact]
    public async Task EnumerateMessages_NoMessagesFound()
    {
        _conversationServiceMock.Setup(m => m.EnumerateMessagesInAConversation(_conversation1.ConversationId, null, null, null))
            .ThrowsAsync(new MessageNotFoundException());
        var response = await _httpClient.GetAsync($"Conversation/{_conversation1.ConversationId}/messages");
        var json = await response.Content.ReadAsStringAsync();
        var listMessageResponse = JsonConvert.DeserializeObject<ListMessageResponse>(json);
        
        Assert.Equal(HttpStatusCode.OK,response.StatusCode );
        Assert.Equal(new List<ListMessageResponseItem>{}, listMessageResponse.Messages);
        _conversationServiceMock.Verify(mock=>mock.EnumerateMessagesInAConversation(_conversation1.ConversationId, null, null, null), Times.Once);
    }
    
    [Fact]
    public async Task StartValidConversation()
    {
        //Setup
        var sendMessageRequest = new SendMessageRequest(_message1.messageId, _message1.senderUsername, _message1.text);
        var participants = new string[]{"foo", "bar"};
        var startConversationRequest = new StartConversationRequest(participants, sendMessageRequest);
        var startConversationResponse =
            new StartConversationResponse(_conversation1.ConversationId, participants, 1000);
        
        _conversationServiceMock.Setup(m => m.StartConversation(It.IsAny<StartConversationRequest>()))
            .ReturnsAsync(startConversationResponse);
        
        //Act
        var response = await _httpClient.PostAsync("Conversation/",
            new StringContent(JsonConvert.SerializeObject(startConversationRequest), Encoding.Default, "application/json"));
        var json = await response.Content.ReadAsStringAsync();
        var actualStartConversationResponse = JsonConvert.DeserializeObject<StartConversationResponse>(json);
        
        //Assert and Verify
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(actualStartConversationResponse.Id, startConversationResponse.Id);
        Assert.Equal(actualStartConversationResponse.Participants, startConversationResponse.Participants);
        Assert.Equal(actualStartConversationResponse.LastModifiedDateUtc, startConversationResponse.LastModifiedDateUtc);
        _conversationServiceMock.Verify(m=>m.StartConversation(It.IsAny<StartConversationRequest>()), Times.Once);
    }
    
    [Fact]
    public async Task StartConversation_AlreadyExists()
    {
        //Setup
        var sendMessageRequest = new SendMessageRequest(_message1.messageId, _message1.senderUsername, _message1.text);
        var participants = new []{"foo", "bar"};
        var startConversationRequest = new StartConversationRequest(participants, sendMessageRequest);
        _conversationServiceMock.Setup(m => m.StartConversation(It.IsAny<StartConversationRequest>()))
            .ThrowsAsync(new ConversationConflictException());

        //Act
        var response = await _httpClient.PostAsync("Conversation/",
            new StringContent(JsonConvert.SerializeObject(startConversationRequest), Encoding.Default, "application/json"));

        //Assert and Verify
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        _conversationServiceMock.Verify(m=>m.StartConversation(It.IsAny<StartConversationRequest>()), Times.Once);
    }
    
    [Fact]
    public async Task StartConversation_InvalidArgs()
    {
        //Setup
        var sendMessageRequest = new SendMessageRequest(_message1.messageId, _message1.senderUsername, _message1.text);
        var participants = new []{"foo", "bar"};
        var startConversationRequest = new StartConversationRequest(participants, sendMessageRequest);
        _conversationServiceMock.Setup(m => m.StartConversation(It.IsAny<StartConversationRequest>()))
            .ThrowsAsync(new ArgumentException());

        //Act
        var response = await _httpClient.PostAsync("Conversation/",
            new StringContent(JsonConvert.SerializeObject(startConversationRequest), Encoding.Default, "application/json"));

        //Assert and Verify
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        _conversationServiceMock.Verify(m=>m.StartConversation(It.IsAny<StartConversationRequest>()), Times.Once);
    }

    [Fact]
    public async Task StartConversation_UserNotFound()
    {
        //Setup
        var sendMessageRequest = new SendMessageRequest(_message1.messageId, _message1.senderUsername, _message1.text);
        var participants = new []{"foo", "bar"};
        var startConversationRequest = new StartConversationRequest(participants, sendMessageRequest);
        _conversationServiceMock.Setup(m => m.StartConversation(It.IsAny<StartConversationRequest>()))
            .ThrowsAsync(new UserNotFoundException());

        //Act
        var response = await _httpClient.PostAsync("Conversation/",
            new StringContent(JsonConvert.SerializeObject(startConversationRequest), Encoding.Default, "application/json"));

        //Assert and Verify
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        _conversationServiceMock.Verify(m=>m.StartConversation(It.IsAny<StartConversationRequest>()), Times.Once);
    }
    
    [Fact]
    public async Task EnumerateConversationsForAUser()
    {
        //Setup
        Profile profile1 = new Profile("mike", "mike", "mike", "testId");
        Profile profile2 = new Profile("bar", "bar", "bar", "testId");
        List<ListConversationsResponseItem> conversationsResponseItems = new List<ListConversationsResponseItem>()
        {
            new ListConversationsResponseItem(_conversation1.ConversationId, _conversation1.LastModifiedUnixTime, profile2),
            new ListConversationsResponseItem("mike_foo", 1001, profile1)
        };
        var encodedToken = HttpUtility.UrlEncode("test/token");
        var encodedResponseToken = HttpUtility.UrlEncode("token/response");
        var nextUri = $"Conversation?username=foo&limit=2&lastSeenConversationTime=1001&continuationToken={encodedResponseToken}";
        
        _conversationServiceMock.Setup(m => m.EnumerateConversationsOfAGivenUser("foo", "test/token", 2, 1001))
            .ReturnsAsync(new ListConversationsServiceResponse(conversationsResponseItems, "token/response"));
        
        //Act
        var response = await _httpClient.GetAsync($"Conversation?username=foo&lastSeenConversationTime=1001&limit=2&continuationToken={encodedToken}");
        var json = await response.Content.ReadAsStringAsync();
        var listConversationsResponse = JsonConvert.DeserializeObject<ListConversationsResponse>(json);
        
        //Assert and Verify
        Assert.Equal(HttpStatusCode.OK,response.StatusCode );
        Assert.Equal(conversationsResponseItems, listConversationsResponse.Conversations);
        Assert.Equal(nextUri, listConversationsResponse.NextUri);
        _conversationServiceMock.Verify(m=>m.EnumerateConversationsOfAGivenUser("foo", "test/token", 2, 1001), Times.Once);
    }
    
    [Fact]
    public async Task EnumerateConversations_UserDoesNotExist()
    {
        //Setup
        _conversationServiceMock.Setup(m => m.EnumerateConversationsOfAGivenUser("foo", null, null, null))
            .ThrowsAsync(new UserNotFoundException());
        
        //Act
        var response = await _httpClient.GetAsync("Conversation?username=foo");
        
        //Assert and Verify
        Assert.Equal(HttpStatusCode.NotFound,response.StatusCode);
        _conversationServiceMock.Verify(mock=>mock.EnumerateConversationsOfAGivenUser("foo", null, null, null), Times.Once);
    }

    [Fact]
    public async Task EnumerateConversations_InvalidArgs()
    {
        //Setup
        _conversationServiceMock.Setup(m => m.EnumerateConversationsOfAGivenUser("foo", null, null, null))
            .ThrowsAsync(new ArgumentException());
        
        //Act
        var response = await _httpClient.GetAsync("Conversation?username=foo");
        
        //Assert and Verify
        Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);
        _conversationServiceMock.Verify(mock=>mock.EnumerateConversationsOfAGivenUser("foo", null, null, null), Times.Once);
    }

    [Fact]
    public async Task EnumerateConversation_ConversationNotFound()
    {
        //Setup
        _conversationServiceMock.Setup(m => m.EnumerateConversationsOfAGivenUser("foo", null, null, null))
            .ThrowsAsync(new ConversationNotFoundException());
        
        //Act
        var response = await _httpClient.GetAsync("Conversation?username=foo");
        var json = await response.Content.ReadAsStringAsync();
        var conversationsResponse = JsonConvert.DeserializeObject<ListConversationsResponse>(json);
        
        //Assert and Verify
        Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        Assert.Equal(new List<ListConversationsResponseItem>(){}, conversationsResponse.Conversations);
        _conversationServiceMock.Verify(mock=>mock.EnumerateConversationsOfAGivenUser("foo", null, null, null), Times.Once);
    }
}