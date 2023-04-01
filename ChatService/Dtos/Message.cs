using System.ComponentModel.DataAnnotations;

namespace ChatService.Dtos;

public record Message([Required] String messageId, 
                        [Required] string text,
                        [Required] string senderUsername,
                        [Required] string conversationId,
                        [Required] long unixTime);