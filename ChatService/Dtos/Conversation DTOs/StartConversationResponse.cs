using System.ComponentModel.DataAnnotations;

namespace ChatService.Dtos;

public record StartConversationResponse([Required] string Id,
                                        [Required] String[] Participants,
                                            [Required] long LastModifiedDateUtc);