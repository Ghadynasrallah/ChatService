using System.ComponentModel.DataAnnotations;

namespace ChatService.Dtos;

public record SendMessageRequest([Required] string MessageId, 
                                    [Required] string SenderUsername,
                                    [Required] string Text);