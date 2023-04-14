using System.ComponentModel.DataAnnotations;

namespace ChatService.Dtos;

public record EnumerateMessagesControllerResponseeDto([Required] List<Message> messages, 
                                                            string? nextUri=null);