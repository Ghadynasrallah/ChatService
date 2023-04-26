using System.ComponentModel.DataAnnotations;

namespace ChatService.Dtos;

public record ListConversationsResponseItem([Required] string Id,
                                [Required] long LastModifiedUnixTime,
                                [Required] Profile Recipient);