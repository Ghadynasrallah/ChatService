using System.ComponentModel.DataAnnotations;

namespace ChatService.Dtos;

public record SendMessageResponse([Required] long createdUnixTime);