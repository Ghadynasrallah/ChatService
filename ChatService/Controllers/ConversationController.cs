using System.Text;
using ChatService.Dtos;
using ChatService.Exceptions;
using ChatService.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChatService.Controllers;

[ApiController]
[Route("api/conversations")]
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
                new { conversationId = message.ConversationId },
                new SendMessageResponse(message.UnixTime));
        }
        catch (ArgumentException)
        {
            return BadRequest($"Invalid message {sendMessageRequest}");
        }
        catch (MessageConflictException)
        {
            return Conflict(
                $"There already exists a message with id {sendMessageRequest.Id} in conversation {conversationId}");
        }
        catch (ConversationNotFoundException)
        {
            return NotFound($"There exists no conversation with ID {conversationId}");
        }
        catch (SenderNotParticipantException)
        {
            return BadRequest(
                $"The sender with username {sendMessageRequest.SenderUsername} is not a participant of the conversation with ID {conversationId}");
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
            string? decodedContinuationToken = null;
            if (continuationToken != null)
            {
                decodedContinuationToken = Encoding.UTF8.GetString(Convert.FromBase64String(continuationToken));
            }
            var messagesStorageResponseDto = await _conversationService.EnumerateMessagesInAConversation(conversationId,
                decodedContinuationToken, limit, lastSeenMessageTime);
            string? nextUri = null;
            if (messagesStorageResponseDto.ContinuationToken != null)
            {
                var encodedContinuationToken =
                    Convert.ToBase64String(Encoding.UTF8.GetBytes(messagesStorageResponseDto.ContinuationToken));
                nextUri =
                    $"api/conversations/{conversationId}/messages?lastSeenMessageTime={lastSeenMessageTime}&limit={limit}&continuationToken={encodedContinuationToken}";
            }
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
    }

    [HttpPost]
    public async Task<ActionResult<AddConversationResponse>> StartConversation(
        [FromBody] AddConversationRequest startConversationRequest)
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
        catch (SenderNotParticipantException)
        {
            return BadRequest($"The sender with username {startConversationRequest.FirstMessage.SenderUsername} is not a participant");
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
            string? decodedContinuationToken = null;
            if (continuationToken != null)
            {
                decodedContinuationToken = Encoding.UTF8.GetString(Convert.FromBase64String(continuationToken));
            }
            var conversationsStorageResponseDto =
                await _conversationService.EnumerateConversationsOfAGivenUser(username, decodedContinuationToken, limit, lastSeenConversationTime);
            string? nextUri = null;
            if (conversationsStorageResponseDto.ContinuationToken != null)
            {
                var encodedContinuationToken =
                    Convert.ToBase64String(Encoding.UTF8.GetBytes(conversationsStorageResponseDto.ContinuationToken));
                nextUri =
                    $"api/conversations?username={username}&limit={limit}&lastSeenConversationTime={lastSeenConversationTime}&continuationToken={encodedContinuationToken}";
            }
            return Ok(new ListConversationsResponse(conversationsStorageResponseDto.Conversations,
                nextUri));
        }
        catch (ArgumentException)
        {
            return BadRequest($"Invalid user ID");
        }
        catch (UserNotFoundException)
        {
            return NotFound($"The user with username {username} was not found");
        }
    }
}