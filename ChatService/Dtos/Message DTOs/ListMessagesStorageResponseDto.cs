using System.ComponentModel.DataAnnotations;

namespace ChatService.Dtos;

public record ListMessagesStorageResponseDto([Required] List<Message> Messages,
                                                    string? ContinuationToken=null);