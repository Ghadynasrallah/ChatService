using System.ComponentModel.DataAnnotations;

namespace ChatService.Dtos;

public record ListMessageResponse([Required] List<ListMessageResponseItem> Messages, 
                                                            string? NextUri=null);