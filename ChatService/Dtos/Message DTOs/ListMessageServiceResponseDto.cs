using System.ComponentModel.DataAnnotations;

namespace ChatService.Dtos;

public record ListMessageServiceResponseDto([Required] List<ListMessageResponseItem> Messages,
                                                        string? ContinuationToken = null);