using System.ComponentModel.DataAnnotations;

namespace ChatService.Dtos;

public record SendMessageRequest([Required] string messageId, 
                                    [Required] string senderUsername,
                                    [Required] string text);