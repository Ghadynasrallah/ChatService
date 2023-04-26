using System.ComponentModel.DataAnnotations;

namespace ChatService.Dtos;

public record AddConversationRequest([Required] string[] Participants,
                                            [Required] SendMessageRequest FirstMessage);