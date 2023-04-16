using System.ComponentModel.DataAnnotations;

namespace ChatService.Dtos;

public record ListMessagesStorageResponseDto([Required] List<ListMessageResponseItem> Messages,
                                                    string? ContinuationToken=null);