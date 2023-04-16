using System.ComponentModel.DataAnnotations;

namespace ChatService.Dtos;

public record Message([Required] string messageId, 
                        [Required] string text,
                        [Required] string senderUsername,
                        [Required] string conversationId,
                        [Required] long unixTime);