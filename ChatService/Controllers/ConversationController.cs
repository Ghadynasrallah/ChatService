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
        try
        {
            var message = new Message(
                Guid.NewGuid().ToString(),
                sendMessageRequest.text,
                sendMessageRequest.senderUsername,
                conversationId,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            );
            _messageStorage.PostMessageToConversation(message);
            return CreatedAtAction(nameof(EnumerateMessagesInAConversation), new {messageId = message.messageId, conversationId = message.conversationId });
        }
        catch (CosmosException e)
        {
            if (e.StatusCode == HttpStatusCode.NotFound)
                return NotFound($"There exists no message with message ID {sendMessageRequest.messageId} in conversation {conversationId}");
            throw;
        }
    }

    [HttpGet("{conversationId}/messages")]
    public async Task<ActionResult<EnumerateMessagesInAConversationResponseDto>> EnumerateMessagesInAConversation([FromRoute] string conversationId)
    {
        List<Message> messages = await _messageStorage.EnumerateMessagesFromAGivenConversation(conversationId);
        if (messages == null)
        {
            return NotFound($"There exists no conversation with conversation ID {conversationId}");
        }
        return Ok(new EnumerateMessagesInAConversationResponseDto(messages));
    }
}