using System.Net;
using ChatService.Dtos;
using ChatService.Exceptions;
using ChatService.Services;
using ChatService.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;

namespace ChatService.Controllers;

[ApiController]
[Route("[Controller]")]
public class ConversationController : ControllerBase
{
    private readonly ConversationService _conversationService;
    public ConversationController(ConversationService conversationService)
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
                                new {messageId = message.messageId, conversationId = message.conversationId },
                                     new SendMessageResponse(message.unixTime));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpGet("{conversationId}/messages")]
    public async Task<ActionResult<EnumerateMessagesControllerResponseeDto>> EnumerateMessagesInAConversation(
        [FromRoute] string conversationId,
        [FromQuery] string? continuationToken = null,
        [FromQuery] int? limit = null,
        [FromQuery] long? lastSeenMessageTime = null)
    {
        try
        {
            var messagesStorageResponseDto = await _conversationService.EnumerateMessagesInAConversation(conversationId,
                continuationToken, limit, lastSeenMessageTime);
            var nextUri =
                $"/api/conversations/{conversationId}/messages?&limit={limit}&lastSeenMessageTime={lastSeenMessageTime}&continuationToken={messagesStorageResponseDto.continuationToken}";
            return Ok(new EnumerateMessagesControllerResponseeDto(messagesStorageResponseDto.messages, nextUri));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (ConversationNotFoundException exception)
        {
            return NotFound(exception.Message);
        }
        catch (MessageNotFoundException)
        {
            return Ok(new EnumerateMessagesControllerResponseeDto(new List<Message>(), null));
        }
    }

    [HttpPost]
    public async Task<ActionResult<StartConversationResponseDto>> StartConversation(
        [FromBody] StartConversationRequestDto startConversationRequestDto)
    {
        try
        {
            var startConversationResponseDto =
                await _conversationService.StartConversation(startConversationRequestDto);
            return CreatedAtAction(nameof(EnumerateConversationsOfAGivenUser),
                new { conversationId = startConversationResponseDto.conversationId },
                startConversationResponseDto);
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
    public async Task<ActionResult<EnumerateConversationsOfAGivenUserDto>> EnumerateConversationsOfAGivenUser(       
        [FromRoute] string userId,
        [FromQuery] string? continuationToken = null,
        [FromQuery] int? limit = null,
        [FromQuery] long? lastSeenConversationTime = null)
    {
        try
        {
            var conversationsStorageResponseDto =
                await _conversationService.EnumerateConversationsOfAGivenUser(userId, continuationToken, limit,
                    lastSeenConversationTime);
            var nextUri =
                $"/api/conversations?username={userId}&limit={limit}&lastSeenConversationTime={lastSeenConversationTime}&continuationToken={conversationsStorageResponseDto.continuationToken}";
            return Ok(new EnumerateConversationsOfAGivenUserDto(conversationsStorageResponseDto.conversations,
                nextUri));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (ConversationNotFoundException exception)
        {
            return NotFound(exception.Message);
        }
        catch (MessageNotFoundException)
        {
            return Ok(new EnumerateConversationsOfAGivenUserDto(new List<Conversation>(), null));
        }
    }
}