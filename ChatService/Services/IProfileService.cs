using ChatService.Dtos;

namespace ChatService.Services;

public interface IProfileService
{
    public Task<Profile> GetProfile(string username);

    public Task AddProfile(Profile profile);

    public Task<Profile> UpdateProfile(string username, PutProfileRequest putProfile);
}