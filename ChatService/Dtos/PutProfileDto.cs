using System.ComponentModel.DataAnnotations;

namespace ChatService.Dtos;

public record PutProfileRequest([Required] string FirstName, [Required] string LastName, string ProfilePictureId);
