using ChatService.Dtos;
using ChatService.Exceptions;
using ChatService.Services;
using ChatService.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using ArgumentException = System.ArgumentException;

namespace ChatService.Controllers;

[ApiController]
[Route("[Controller]")]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpGet("{username}")]
    public async Task<ActionResult<Profile>> GetProfile([FromRoute] string username)
    {
        try
        {
            return Ok(await _profileService.GetProfile(username));
        }
        catch (UserNotFoundException exception)
        {
            return NotFound($"The user with user ID {username} was not found");
        }
        catch (ArgumentException exception)
        {
            return BadRequest($"Invalid username: username cannot be null or empty");
        }
    }

    [HttpPost]
    public async Task<ActionResult<Profile>> AddProfile(Profile profile)
    {
        try
        {
            await _profileService.AddProfile(profile);
            return CreatedAtAction(nameof(GetProfile), new { username = profile.Username }, profile);
        }
        catch (ArgumentException)
        {
            return BadRequest($"Invalid profile arguments {profile}");
        }
        catch (UserConflictException)
        {
            return Conflict($"A user with username {profile.Username} already exists");
        }
    }

    [HttpPut("{username}")]
    public async Task<ActionResult<Profile>> UpdateProfile(string username, [FromBody] PutProfileRequest putProfile)
    {
        try
        {
            return Ok(await _profileService.UpdateProfile(username, putProfile));
        }
        catch (ArgumentException)
        {
            return BadRequest($"Invalid Arguments: Username, FirstName, LastName cannot be null or empty");
        }
        catch (UserNotFoundException)
        {
            return NotFound($"The user with username {username} was not found");
        }
    }
}