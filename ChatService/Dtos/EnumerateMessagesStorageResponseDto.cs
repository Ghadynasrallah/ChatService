using System.ComponentModel.DataAnnotations;

namespace ChatService.Dtos;

public record EnumerateMessagesStorageResponseDto([Required] List<Message> messages,
                                                    string? continuationToken=null);