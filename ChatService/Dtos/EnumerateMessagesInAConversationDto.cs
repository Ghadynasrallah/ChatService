using System.ComponentModel.DataAnnotations;

namespace ChatService.Dtos;

public record EnumerateMessagesInAConversationDto([Required] List<Message> messages);