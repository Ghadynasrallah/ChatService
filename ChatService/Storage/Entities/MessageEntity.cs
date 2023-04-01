using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.Cosmos;

namespace ChatService.Storage.Entities;

public record MessageEntity([Required] string partitionKey, 
                            [Required] string id,
                            [Required] string text,
                            [Required] string senderUsername,
                            [Required] long unixTime);