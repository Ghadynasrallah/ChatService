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
        _messageStorageMock.Verify(_messageStorageMock => _messageStorageMock.PostMessageToConversation(It.IsAny<Message>()), Times.Once);
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

        _messageStorageMock.Setup(m => m.EnumerateMessagesFromAGivenConversation("foo_bar"))
            .ReturnsAsync(messages);
        var response = await _httpClient.GetAsync("Conversation/foo_bar/messages");

        Assert.Equal(response.StatusCode ,HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Equal(messages, JsonConvert.DeserializeObject<EnumerateMessagesInAConversationResponseDto>(json).messages);
        _messageStorageMock.Verify(_messageStorageMock=>_messageStorageMock.EnumerateMessagesFromAGivenConversation("foo_bar"), Times.Once);
    }

    [Fact]
    public async Task EnumerateMessagesInANotFoundConversation()
    {
        var response = await _httpClient.GetAsync("Conversation/foo_bar/messages");
        _messageStorageMock.Setup(m => m.EnumerateMessagesFromAGivenConversation("foo_bar"))
            .ReturnsAsync((List<Message>?) null);
        Assert.Equal(response.StatusCode ,HttpStatusCode.NotFound);
        _messageStorageMock.Verify(_messageStorageMock=>_messageStorageMock.EnumerateMessagesFromAGivenConversation("foo_bar"), Times.Once);
    }
}