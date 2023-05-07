using System.ComponentModel.DataAnnotations;

namespace ChatService.Dtos;

public record ListMessageResponseItem([Required] string Text,
                                [Required] string SenderUsername,
                                [Required] long UnixTime);