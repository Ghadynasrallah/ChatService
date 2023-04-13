using System.ComponentModel.DataAnnotations;

namespace ChatService.Dtos;

public record EnumerateConversationsOfAGivenUserDto([Required] List<Conversation>? conversations,
                                                    string nextUri);