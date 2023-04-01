using System.ComponentModel.DataAnnotations;

namespace ChatService.Dtos;

public record Message([Required] int messageId, 
                        [Required] string text,
                        [Required] string senderUsername,
                        [Required] int conversationId,
                        [Required] long unixTime);