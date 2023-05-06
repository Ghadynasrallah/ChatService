using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.Cosmos.Serialization.HybridRow;

namespace ChatService.Dtos;

public record Conversation([Required] string conversationId, 
                            [Required] string userId1,
                            [Required] string userId2,
                            [Required] long lastModifiedUnixTime);