using System.ComponentModel.DataAnnotations;

namespace ChatService.Dtos;

public record StartConversationRequestDto([Required] string userId1,
                                            [Required] string userId2,
                                            [Required] SendMessageRequest sendMessageRequest);