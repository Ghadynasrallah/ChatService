using System.ComponentModel.DataAnnotations;

namespace ChatService.Dtos;

public record EnumerateMessagesInAConversationResponseDto([Required] List<Message> messages, 
                                                            string? nextUri=null);