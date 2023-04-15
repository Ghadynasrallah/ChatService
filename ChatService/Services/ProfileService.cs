using ChatService.Dtos;
using ChatService.Exceptions;
using ChatService.Storage;

namespace ChatService.Services;

public class ProfileService
{
    private readonly IProfileStorage _profileStorage;

    public ProfileService(IProfileStorage profileStorage)
    {
        _profileStorage = profileStorage;
    }

    public async Task<Profile> GetProfile(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException($"Invalid username: username cannot be null or empty");
        }
        var profile = await _profileStorage.GetProfile(username);
        if (profile == null)
        {
            throw new UserNotFoundException($"The user with username {username} was not found");
        }
        return profile;
    }

    public async Task AddProfile(Profile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.Username) ||
            string.IsNullOrWhiteSpace(profile.FirstName) ||
            string.IsNullOrWhiteSpace(profile.LastName))
        {
            throw new ArgumentException($"Invalid profile {profile}", nameof(profile));
        }

        var existingProfile = await _profileStorage.GetProfile(profile.Username);
        if (existingProfile != null)
        {
            throw new UserConflictException($"A user with username {profile.Username} already exists");
        }

        await _profileStorage.UpsertProfile(profile);
    }

    public async Task<Profile> UpdateProfile(string username, PutProfileRequest putProfile)
    {
        var updatedProfile = new Profile(username, putProfile.FirstName, putProfile.LastName,
            putProfile.ProfilePictureId);
        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(putProfile.FirstName) ||
            string.IsNullOrWhiteSpace(putProfile.LastName))
        {
            throw new ArgumentException($"Invalid profile {updatedProfile}", nameof(updatedProfile));
        }
        var oldProfile = await _profileStorage.GetProfile(username);
        if (oldProfile == null)
        {
            throw new UserNotFoundException($"The user with username {username} was not found");
        }
        await _profileStorage.UpsertProfile(updatedProfile);
        return updatedProfile;
    }
}