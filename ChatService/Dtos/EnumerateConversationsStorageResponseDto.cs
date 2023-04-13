using System.ComponentModel.DataAnnotations;

namespace ChatService.Dtos;

public record EnumerateConversationsStorageResponseDto([Required] List<Conversation> conversations,
                                                        string? continuationToken = null);