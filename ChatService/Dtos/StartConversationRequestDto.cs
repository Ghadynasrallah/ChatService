using System.ComponentModel.DataAnnotations;

namespace ChatService.Dtos;

public record StartConversationRequestDto([Required] List<String> participants,
                                            [Required] SendMessageRequest firstMessage);