using System.ComponentModel.DataAnnotations;

namespace ChatService.Dtos;

public record AddConversationResponse([Required] string Id,
                                        [Required] String[] Participants,
                                            [Required] DateTime LastModifiedDateUtc);