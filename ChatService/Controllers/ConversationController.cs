using System.Diagnostics;
using System.Text;
using ChatService.Dtos;
using ChatService.Exceptions;
using ChatService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ApplicationInsights;

namespace ChatService.Controllers;

[ApiController]
[Route("api/conversations")]
public class ConversationController : ControllerBase
{
    private readonly IConversationService _conversationService;
    private readonly ILogger<ConversationController> _logger;
    private readonly TelemetryClient _telemetryClient;

    public ConversationController(
        IConversationService conversationService,
        ILogger<ConversationController> logger,
        TelemetryClient telemetryClient)
    {
        _conversationService = conversationService;
        _logger = logger;
        _telemetryClient = telemetryClient;
    }

    [HttpPost("{conversationId}/messages")]
    public async Task<ActionResult<SendMessageResponse>> SendMessageToConversation([FromRoute] string conversationId, [FromBody] SendMessageRequest sendMessageRequest)
    {
        using (_logger.BeginScope(new Dictionary<string, object>
               {
                   { "ConversationId", conversationId },
                   { "SenderUsername", sendMessageRequest.SenderUsername }
               }))
        {
            try
            {
                var stopWatch = Stopwatch.StartNew();
                var message = await _conversationService.SendMessageToConversation(conversationId, sendMessageRequest);
                _logger.LogInformation(
                    "Message sent successfully. Conversation ID: {ConversationId}, Message ID: {MessageId}",
                    conversationId, message.MessageId);
                _telemetryClient.TrackEvent("MessageSent");
                _telemetryClient.TrackMetric("MessageStore.SendMessage.Time", stopWatch.ElapsedMilliseconds);
                return CreatedAtAction(nameof(EnumerateMessagesInAConversation),
                    new { conversationId = message.ConversationId },
                    new SendMessageResponse(message.UnixTime));
            }
            catch (ArgumentException)
            {
                _logger.LogWarning("Invalid message arguments {SendMessageRequest}", sendMessageRequest);
                return BadRequest($"Invalid message {sendMessageRequest}");
            }
            catch (MessageConflictException)
            {
                _logger.LogWarning(
                    "There already exists a message with id {MessageId} in conversation {ConversationId}",
                    sendMessageRequest.Id, conversationId);
                return Conflict(
                    $"There already exists a message with id {sendMessageRequest.Id} in conversation {conversationId}");
            }
            catch (ConversationNotFoundException)
            {
                _logger.LogWarning("There exists no conversation with ID {ConversationId}", conversationId);
                return NotFound($"There exists no conversation with ID {conversationId}");
            }
            catch (SenderNotParticipantException)
            {
                _logger.LogWarning(
                    "The sender with username {SenderUsername} is not a participant of the conversation with ID {ConversationId}",
                    sendMessageRequest.SenderUsername, conversationId);
                return BadRequest(
                    $"The sender with username {sendMessageRequest.SenderUsername} is not a participant of the conversation with ID {conversationId}");
            }
        }
    }

    [HttpGet("{conversationId}/messages")]
    public async Task<ActionResult<ListMessageResponse>> EnumerateMessagesInAConversation(
        [FromRoute] string conversationId,
        [FromQuery] string? continuationToken = null,
        [FromQuery] int? limit = null,
        [FromQuery] long? lastSeenMessageTime = null)
    {
        using (_logger.BeginScope("{ConversationId}", conversationId))
        {
            try
            {
                var stopWatch = Stopwatch.StartNew();
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
                _logger.LogInformation("Messages enumerated successfully for conversation: {ConversationId}", conversationId);
                _telemetryClient.TrackMetric("MessageStore.EnumerateMessages.Time", stopWatch.ElapsedMilliseconds);
                return Ok(new ListMessageResponse(messagesStorageResponseDto.Messages, nextUri));
            }
            catch (ArgumentException)
            {
                _logger.LogWarning("Invalid conversation ID: {ConversationId}", conversationId);
                return BadRequest($"Invalid conversation ID {conversationId}");
            }
            catch (ConversationNotFoundException)
            {
                _logger.LogWarning("Conversation not found: {ConversationId}", conversationId);
                return NotFound($"There exists no conversation with ID {conversationId}");
            }
        }
    }

    [HttpPost]
    public async Task<ActionResult<AddConversationResponse>> StartConversation(
        [FromBody] AddConversationRequest startConversationRequest)
    {
        var userId1 = startConversationRequest.Participants[0];
        var userId2 = startConversationRequest.Participants[1];
        using (_logger.BeginScope(new Dictionary<string, object>
               {
                   { "UserId1", userId1},
                   { "UserId2", userId2 }
               }))
        {
            try
            {
                var stopWatch = Stopwatch.StartNew();
                var startConversationResponse =
                    await _conversationService.StartConversation(startConversationRequest);
                _logger.LogInformation("Conversation started successfully. Conversation ID: {ConversationId}",
                    startConversationResponse.Id);
                _telemetryClient.TrackEvent("ConversationAdded");
                _telemetryClient.TrackMetric("ConversationStore.StartConversation.Time", stopWatch.ElapsedMilliseconds);
                return CreatedAtAction(nameof(EnumerateConversationsOfAGivenUser),
                    new { userId = startConversationResponse.Participants[0] },
                    startConversationResponse);
            }
            catch (ArgumentException exception)
            {
                _logger.LogWarning("Invalid Arguments {AddConversationRequest}", startConversationRequest);
                return BadRequest(exception.Message);
            }
            catch (UserNotFoundException exception)
            {
                _logger.LogWarning("User not found. UserId 1: {UserId1}, UserId 2: {UserId2}", userId1, userId2);
                return NotFound(exception.Message);
            }
            catch (ConversationConflictException exception)
            {
                _logger.LogWarning("Conversation conflict. UserId 1: {UserId1}, UserId 2: {UserId2}", userId1, userId2);
                return Conflict(exception.Message);
            }
            catch (SenderNotParticipantException)
            {
                _logger.LogWarning("Sender {SenderUsername} is not a participant",
                    startConversationRequest.FirstMessage.SenderUsername);
                return BadRequest(
                    $"The sender with username {startConversationRequest.FirstMessage.SenderUsername} is not a participant");
            }
        }
    }

    [HttpGet]
    public async Task<ActionResult<ListConversationsResponse>> EnumerateConversationsOfAGivenUser(       
        [FromQuery] string username,
        [FromQuery] string? continuationToken = null,
        [FromQuery] int? limit = null,
        [FromQuery] long? lastSeenConversationTime = null)
    {
        using (_logger.BeginScope("Username", username))
        {
            try
            {
                var stopWatch = Stopwatch.StartNew();
                string? decodedContinuationToken = null;
                if (continuationToken != null)
                {
                    decodedContinuationToken = Encoding.UTF8.GetString(Convert.FromBase64String(continuationToken));
                }

                var conversationsStorageResponseDto =
                    await _conversationService.EnumerateConversationsOfAGivenUser(username, decodedContinuationToken,
                        limit, lastSeenConversationTime);
                string? nextUri = null;
                if (conversationsStorageResponseDto.ContinuationToken != null)
                {
                    var encodedContinuationToken =
                        Convert.ToBase64String(
                            Encoding.UTF8.GetBytes(conversationsStorageResponseDto.ContinuationToken));
                    nextUri =
                        $"api/conversations?username={username}&limit={limit}&lastSeenConversationTime={lastSeenConversationTime}&continuationToken={encodedContinuationToken}";
                }
                _logger.LogInformation("Conversations enumerated successfully for user: {Username}", username);
                _telemetryClient.TrackMetric("ConversationStore.EnumerateConversations.Time", stopWatch.ElapsedMilliseconds);
                return Ok(new ListConversationsResponse(conversationsStorageResponseDto.Conversations,
                    nextUri));
            }
            catch (ArgumentException)
            {
                _logger.LogWarning("Invalid user ID");
                return BadRequest($"Invalid user ID");
            }
            catch (UserNotFoundException)
            {
                _logger.LogWarning("User not found: {Username}", username);
                return NotFound($"The user with username {username} was not found");
            }
        }
    }
}