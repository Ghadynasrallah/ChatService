using ChatService.Dtos;
using ChatService.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using ArgumentException = System.ArgumentException;

namespace ChatService.Controllers;

[ApiController]
[Route("[Controller]")]
public class ProfileController : ControllerBase
{
    private readonly IProfileStorage _profileStorage;

    public ProfileController(IProfileStorage profileStorage)
    {
        _profileStorage = profileStorage;
    }

    [HttpGet("{username}")]
    public async Task<ActionResult<Profile>> GetProfile([FromRoute] string username)
    {
        var profile = await _profileStorage.GetProfile(username);
        if (profile == null)
        {
            return NotFound($"A user with username {username} was not found");
        }

        return Ok(profile);
    }

    [HttpPost]
    public async Task<ActionResult<Profile>> AddProfile(Profile profile)
    {
        try
        {
            var existingProfile = await _profileStorage.GetProfile(profile.username);
            if (existingProfile != null)
            {
                return Conflict($"A user with username {profile.username} already exists");
            }

            await _profileStorage.UpsertProfile(profile);
            return CreatedAtAction(nameof(GetProfile), new { username = profile.username }, profile);
        }
        catch (ArgumentException)
        {
            return BadRequest($"Invalid profile {profile}");
        }
    }

    [HttpPut("{username}")]
    public async Task<ActionResult<Profile>> UpdateProfile(String username, [FromBody] PutProfileRequest putProfile)
    {
        try
        {
            var existingProfile = await _profileStorage.GetProfile(username);
            if (existingProfile == null)
            {
                return NotFound($"A user with username {username} does not exist");
            }

            var updatedProfile = new Profile(username, putProfile.FirstName, putProfile.LastName,
                putProfile.ProfilePictureId);
            await _profileStorage.UpsertProfile(updatedProfile);
            return Ok(updatedProfile);
        }
        catch (ArgumentException)
        {
            var updatedProfile = new Profile(username, putProfile.FirstName, putProfile.LastName,
                putProfile.ProfilePictureId);
            return BadRequest($"Invalid profile {updatedProfile}");
        }
    }
    
}