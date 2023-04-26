using System.ComponentModel.DataAnnotations;

namespace ChatService.Dtos;

public record Message([Required] string MessageId, 
                        [Required] string Text,
                        [Required] string SenderUsername,
                        [Required] string ConversationId,
                        [Required] long UnixTime);