
namespace ChatService.Storage.Entities;

public record ProfileEntity(string partitionKey, string id, string FirstName, string LastName, string ProfilePictureId);
