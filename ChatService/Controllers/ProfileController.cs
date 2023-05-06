using ChatService.Dtos;
using ChatService.Exceptions;
using ChatService.Services;
using Microsoft.AspNetCore.Mvc;
using ArgumentException = System.ArgumentException;

namespace ChatService.Controllers;

[ApiController]
[Route("api/profile")]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(IProfileService profileService, ILogger<ProfileController> logger)
    {
        _profileService = profileService;
        _logger = logger;
    }

    [HttpGet("{username}")]
    public async Task<ActionResult<Profile>> GetProfile([FromRoute] string username)
    {
        using (_logger.BeginScope("{Username}", username))
        {
            try
            {
                var profile = await _profileService.GetProfile(username);
                _logger.LogInformation("Profile retrieved successfully. User ID: {ProfileUsername}", username);
                return Ok(profile);
            }
            catch (UserNotFoundException)
            {
                _logger.LogWarning("User with id {ProfileUsername} was not found", username);
                return NotFound($"The user with user ID {username} was not found");
            }
            catch (ArgumentException)
            {
                _logger.LogWarning("Invalid username: username cannot be null or empty");
                return BadRequest($"Invalid username: username cannot be null or empty");
            }
        }
    }

    [HttpPost]
    public async Task<ActionResult<Profile>> AddProfile(Profile profile)
    {
        using (_logger.BeginScope("{Username}", profile.Username))
        {
            try
            {
                await _profileService.AddProfile(profile);
                _logger.LogInformation("Added profile with username {ProfileUsername}", profile.Username);
                return CreatedAtAction(nameof(GetProfile), new { username = profile.Username }, profile);
            }
            catch (ArgumentException)
            {
                _logger.LogWarning("Invalid profile arguments {Profile}", profile);
                return BadRequest($"Invalid profile arguments {profile}");
            }
            catch (UserConflictException)
            {
                _logger.LogWarning("A user with username {ProfileUsername} already exists", profile.Username);
                return Conflict($"A user with username {profile.Username} already exists");
            }
            catch
            {
                _logger.LogError("An error occured while adding the profile with username {ProfileUsername}", profile.Username);
                throw;
            }
        }
    }

    [HttpPut("{username}")]
    public async Task<ActionResult<Profile>> UpdateProfile(string username, [FromBody] PutProfileRequest putProfile)
    {
        using (_logger.BeginScope("{Username}", username))
        {
            try
            {
                var updatedProfile = await _profileService.UpdateProfile(username, putProfile);
                _logger.LogInformation("Updated profile with username {ProfileUsername}", username);
                return Ok(updatedProfile);
            }
            catch (ArgumentException)
            {
                _logger.LogWarning("Invalid profile arguments {PutProfile}", putProfile);
                return BadRequest($"Invalid Arguments: Username, FirstName, and LastName cannot be null or empty");
            }
            catch (UserNotFoundException)
            {
                _logger.LogWarning("The user with username {username} was not found", username);
                return NotFound($"The user with username {username} was not found");
            }
            catch
            {
                _logger.LogError("There was an error updating the profile with username {ProfileUsername}", username);
                throw;
            }
        }
    }
}