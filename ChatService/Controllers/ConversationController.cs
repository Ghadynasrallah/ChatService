using System.Net;
using ChatService.Dtos;
using ChatService.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;

namespace ChatService.Controllers;

[ApiController]
[Route("[Controller]")]
public class ConversationController : ControllerBase
{
    private readonly IConversationStorage _conversationStorage;
    private readonly IMessageStorage _messageStorage;

    public ConversationController(IConversationStorage conversationStorage, IMessageStorage messageStorage)
    {
        _conversationStorage = conversationStorage;
        _messageStorage = messageStorage;
    }

    [HttpPost("{conversationId}/messages")]
    public async Task<ActionResult<SendMessageResponse>> SendMessageToConversation([FromRoute] string conversationId, [FromBody] SendMessageRequest sendMessageRequest)
    {
        var message = new Message(
            sendMessageRequest.messageId,
            sendMessageRequest.text,
            sendMessageRequest.senderUsername,
            conversationId,
            DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        );
        try
        {
            await _messageStorage.PostMessageToConversation(message);
            return CreatedAtAction(nameof(EnumerateMessagesInAConversation),
                                new {messageId = message.messageId, conversationId = message.conversationId },
                                     new SendMessageResponse(message.unixTime));
        }
        catch (ArgumentException)
        {
            return BadRequest($"Invalid message arguments {message}");
        }
    }

    [HttpGet("{conversationId}/messages")]
    public async Task<ActionResult<EnumerateMessagesInAConversationResponseDto>> EnumerateMessagesInAConversation([FromRoute] string conversationId,
        [FromQuery] string? continuationToken = null,
        [FromQuery] int? limit = null,
        [FromQuery] long? lastSeenMessageTime = null)
    {
        var response = await _messageStorage.EnumerateMessagesFromAGivenConversation(conversationId, continuationToken, limit, lastSeenMessageTime);
        if (response == null)
        {
            return NotFound($"There exists no conversation with conversation ID {conversationId}");
        }
        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<StartConversationResponseDto>> StartConversation(
        [FromBody] StartConversationRequestDto startConversationRequestDto)
    {
        string userId1 = startConversationRequestDto.userId1;
        string userId2 = startConversationRequestDto.userId2;
        var sendMessageRequest = startConversationRequestDto.sendMessageRequest;

        var conversation = new Conversation(userId1, userId2,
            DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        if (await _conversationStorage.GetConversation(conversation.userId1, conversation.userId2) != null)
        {
            return Conflict($"There already exists a conversation between {userId1} and {userId1}");
        }
        
        try
        {
            string conversationId = await _conversationStorage.PostConversation(conversation);
            var message = new Message(
                sendMessageRequest.messageId,
                sendMessageRequest.text,
                sendMessageRequest.senderUsername,
                conversationId,
                conversation.lastModifiedUnixTime
            );
            bool sendFirstMessage = await SendFirstMessageWhenStartingAConversation(message);
            if (!sendFirstMessage)
            {
                return BadRequest($"Invalid message arguments {message}");
            }
            var startConversationResponse = new StartConversationResponseDto(conversationId, conversation.lastModifiedUnixTime);
            return CreatedAtAction(nameof(EnumerateConversationsOfAGivenUser), 
                                new { conversationId = conversationId },
                                startConversationResponse);
        }
        catch (ArgumentException)
        {
            return BadRequest($"Invalid start conversation request arguments {conversation}");
        }
    }

    [HttpGet]
    public async Task<ActionResult<EnumerateConversationsOfAGivenUserDto>> EnumerateConversationsOfAGivenUser([FromQuery] string userId) 
    {
        EnumerateConversationsStorageResponseDto? response = await _conversationStorage.EnumerateConversationsForAGivenUser(userId);
        if (response == null)
        {
            return NotFound($"There exists no user with user ID {userId}");
        }

        return Ok(response);
    }

    private async Task<bool> SendFirstMessageWhenStartingAConversation(Message message)
    {
        try
        {
            await _messageStorage.PostMessageToConversation(message);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}