using System.ComponentModel.DataAnnotations;

namespace ChatService.Dtos;

public record StartConversationRequest([Required] string[] Participants,
                                            [Required] SendMessageRequest FirstMessage);