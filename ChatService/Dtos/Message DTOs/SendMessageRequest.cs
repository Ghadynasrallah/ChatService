using System.ComponentModel.DataAnnotations;

namespace ChatService.Dtos;

public record SendMessageRequest([Required] string Id, 
                                    [Required] string SenderUsername,
                                    [Required] string Text);