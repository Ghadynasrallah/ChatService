using System.Web;
using ChatService.Dtos;
using ChatService.Exceptions;
using ChatService.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChatService.Controllers;

[ApiController]
[Route("[Controller]")]
public class ConversationController : ControllerBase
{
    private readonly IConversationService _conversationService;
    public ConversationController(IConversationService conversationService)
    {
        _conversationService = conversationService;
    }

    [HttpPost("{conversationId}/messages")]
    public async Task<ActionResult<SendMessageResponse>> SendMessageToConversation([FromRoute] string conversationId, [FromBody] SendMessageRequest sendMessageRequest)
    {
        try
        {
            var message = await _conversationService.SendMessageToConversation(conversationId, sendMessageRequest);
            return CreatedAtAction(nameof(EnumerateMessagesInAConversation),
                                new {conversationId = message.conversationId },
                                     new SendMessageResponse(message.unixTime));
        }
        catch (ArgumentException)
        {
            return BadRequest($"Invalid message {sendMessageRequest}");
        }
    }
    

    [HttpGet("{conversationId}/messages")]
    public async Task<ActionResult<ListMessageResponse>> EnumerateMessagesInAConversation(
        [FromRoute] string conversationId,
        [FromQuery] string? continuationToken = null,
        [FromQuery] int? limit = null,
        [FromQuery] long? lastSeenMessageTime = null)
    {
        try
        {
            var messagesStorageResponseDto = await _conversationService.EnumerateMessagesInAConversation(conversationId,
                HttpUtility.UrlDecode(continuationToken), limit, lastSeenMessageTime);
            var encodedContinuationToken = HttpUtility.UrlEncode(messagesStorageResponseDto.ContinuationToken);
            var nextUri =
                $"Conversation/{conversationId}/messages?lastSeenMessageTime={lastSeenMessageTime}&limit={limit}&continuationToken={encodedContinuationToken}";
            return Ok(new ListMessageResponse(messagesStorageResponseDto.Messages, nextUri));
        }
        catch (ArgumentException)
        {
            return BadRequest($"Invalid conversation ID {conversationId}");
        }
        catch (ConversationNotFoundException)
        {
            return NotFound($"There exists no conversation with ID {conversationId}");
        }
        catch (MessageNotFoundException)
        {
            return Ok(new ListMessageResponse(new List<ListMessageResponseItem>(), null));
        }
    }

    [HttpPost]
    public async Task<ActionResult<StartConversationResponse>> StartConversation(
        [FromBody] StartConversationRequest startConversationRequest)
    {
        try
        {
            var startConversationResponse =
                await _conversationService.StartConversation(startConversationRequest);
            return CreatedAtAction(nameof(EnumerateConversationsOfAGivenUser),
                new { userId = startConversationResponse.Participants[0] },
                startConversationResponse);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (UserNotFoundException exception)
        {
            return NotFound(exception.Message);
        }
        catch (ConversationConflictException exception)
        {
            return Conflict(exception.Message);
        } 
    }

    [HttpGet]
    public async Task<ActionResult<ListConversationsResponse>> EnumerateConversationsOfAGivenUser(       
        [FromQuery] string username,
        [FromQuery] string? continuationToken = null,
        [FromQuery] int? limit = null,
        [FromQuery] long? lastSeenConversationTime = null)
    {
        try
        {
            var conversationsStorageResponseDto =
                await _conversationService.EnumerateConversationsOfAGivenUser(username, HttpUtility.UrlDecode(continuationToken), limit, lastSeenConversationTime);
            var encodedResponseToken = HttpUtility.UrlEncode(conversationsStorageResponseDto.ContinuationToken);
            var nextUri =
                $"Conversation?username={username}&limit={limit}&lastSeenConversationTime={lastSeenConversationTime}&continuationToken={encodedResponseToken}";
            return Ok(new ListConversationsResponse(conversationsStorageResponseDto.Conversations,
                nextUri));
        }
        catch (ArgumentException)
        {
            return BadRequest($"Invalid user ID");
        }
        catch (ConversationNotFoundException)
        {
            return Ok(new ListConversationsResponse(new List<ListConversationsResponseItem>()));
        }
        catch (UserNotFoundException)
        {
            return NotFound($"The user with username {username} was not found");
        }
    }
}