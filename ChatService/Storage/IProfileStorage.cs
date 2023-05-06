using ChatService.Dtos;
namespace ChatService.Storage;

public interface IProfileStorage
{
    public Task UpsertProfile(Profile profile);

    public Task<Profile?> GetProfile(string username);
    
    public Task DeleteProfile(string username);
}
