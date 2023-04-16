using System.ComponentModel.DataAnnotations;

namespace ChatService.Dtos;

public record ListConversationsStorageResponse([Required] List<Conversation> Conversations,
                                                        string? ContinuationToken = null);