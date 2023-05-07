using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.Cosmos;

namespace ChatService.Dtos;

public record ListConversationsResponse([Required] List<ListConversationsResponseItem> Conversations,
                                                    string? NextUri=null);