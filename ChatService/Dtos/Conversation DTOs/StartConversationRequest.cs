using System.ComponentModel.DataAnnotations;

namespace ChatService.Dtos;

public record StartConversationRequest([Required] String[] Participants,
                                            [Required] SendMessageRequest FirstMessage);