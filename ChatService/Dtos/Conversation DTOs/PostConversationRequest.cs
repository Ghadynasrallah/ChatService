using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.Cosmos.Serialization.HybridRow;

namespace ChatService.Dtos;

public record PostConversationRequest([Required] string UserId1,
                            [Required] string UserId2,
                            [Required] long LastModifiedUnixTime);