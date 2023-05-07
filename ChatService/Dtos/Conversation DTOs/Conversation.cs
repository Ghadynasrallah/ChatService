using System.ComponentModel.DataAnnotations;

namespace ChatService.Dtos;

public record Conversation([Required] string ConversationId, 
                                    [Required] string UserId1,      
                                    [Required] string UserId2,                                     
                                    [Required] long LastModifiedUnixTime);