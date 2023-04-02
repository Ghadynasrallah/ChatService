using System.ComponentModel.DataAnnotations;

namespace ChatService.Dtos;

public record StartConversationResponseDto([Required] string conversationId,
                                            [Required] long createdUnixTime);