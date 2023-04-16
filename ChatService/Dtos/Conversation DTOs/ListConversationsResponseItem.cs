using System.ComponentModel.DataAnnotations;

namespace ChatService.Dtos;

public record ListConversationsResponseItem([Required] string ConversationId,
                                [Required] long LastModifiedUnixTime,
                                [Required] Profile Recipient);