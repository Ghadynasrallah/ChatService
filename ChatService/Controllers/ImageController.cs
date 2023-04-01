using ChatService.Dtos;
using ChatService.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;


namespace ChatService.Controllers;

[ApiController]
[Route("[Controller]")]
public class ImageController : ControllerBase
{
    private readonly IProfilePictureStorage _profilePictureStorage;

    public ImageController(IProfilePictureStorage profilePictureStorage)
    {
        _profilePictureStorage = profilePictureStorage;
    }

    [HttpPost]
    public async Task<ActionResult<UploadImageResponse>> UploadImage([FromForm] UploadImageRequest uploadImageRequest)
    {
        try
        {
            var imageId = await _profilePictureStorage.UploadImage(uploadImageRequest.File.OpenReadStream());
            return Ok(new UploadImageResponse(imageId));
        }
        catch (ArgumentException)
        {
            return BadRequest("The Image file provided is empty");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult> DownloadImage([FromRoute] string id)
    {
        var imageData = await _profilePictureStorage.DownloadImage(id);
        
        if (imageData == null)
        {
            return NotFound($"The image with id {id} was not found");
        }
    
        var imageMemoryStream = new MemoryStream();
        await imageData.CopyToAsync(imageMemoryStream);
        imageMemoryStream.Position = 0;
        //return Ok(imageMemoryStream);
        return Ok(new FileContentResult(imageMemoryStream.ToArray(), "image/jpeg"));
    }
}