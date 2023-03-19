using ChatService.Dtos;
using ChatService.Storage;
using Microsoft.AspNetCore.Mvc;


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
        if (uploadImageRequest.File.Length == 0)
        {
            return BadRequest("Image file is empty");
        }
        var imageId = await _profilePictureStorage.UploadImage(uploadImageRequest.File.OpenReadStream());
        return Ok(new UploadImageResponse(imageId));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult> DownloadImage(string id)
    {
        if (String.IsNullOrWhiteSpace(id))
        {
            return BadRequest("The ID provided contains no text");
        }
        var imageData = await _profilePictureStorage.DownloadImage(id);
        if (imageData == null)
        {
            return NotFound($"The image with id {id} was not found");
        }
        
        var memoryStream = new MemoryStream();
        await imageData.CopyToAsync(memoryStream);
        memoryStream.Position = 0;
        return Ok(new FileContentResult(memoryStream.ToArray(), "image/jpeg"));
    }
}