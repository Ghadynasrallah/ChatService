using System.ComponentModel.DataAnnotations;

namespace ChatService.Storage.Entities;

public record ConversationEntity([Required] string partitionKey,
                                    [Required] string id,
                                    [Required] string userId1,
                                    [Required] string userId2,
                                    [Required] long lastModifiedUnixTime);