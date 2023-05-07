using System.ComponentModel.DataAnnotations;

namespace ChatService.Dtos;

public record ListConversationsServiceResponse([Required] List<ListConversationsResponseItem> Conversations,
                                                    string? ContinuationToken = null);