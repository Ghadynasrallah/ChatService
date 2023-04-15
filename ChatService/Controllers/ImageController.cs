using ChatService.Dtos;
using ChatService.Exceptions;
using ChatService.Services;
using ChatService.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;


namespace ChatService.Controllers;

[ApiController]
[Route("[Controller]")]
public class ImageController : ControllerBase
{
    private readonly ImageService _imageService;

    public ImageController(ImageService imageService)
    {
        _imageService = imageService;
    }

    [HttpPost]
    public async Task<ActionResult<UploadImageResponse>> UploadImage([FromForm] UploadImageRequest uploadImageRequest)
    {
        try
        {
            var profilePictureData = uploadImageRequest.File.OpenReadStream();
            var imageId = await _imageService.UploadImage(profilePictureData);
            return Ok(new UploadImageResponse(imageId));
        }
        catch (ArgumentException)
        {
            return BadRequest("The Image file provided is empty");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult> DownloadImage([FromRoute] string imageId)
    {
        try
        {
            var imageData = await _imageService.DownloadImage(imageId);
            var imageMemoryStream = new MemoryStream();
            await imageData.CopyToAsync(imageMemoryStream);
            imageMemoryStream.Position = 0;
            return new FileContentResult(imageMemoryStream.ToArray(), "image/jpeg");
        }
        catch (ArgumentException)
        {
            return BadRequest("Invalid Argument: Image ID cannot be null or empty");
        }
        catch (ImageNotFoundException)
        {
            return NotFound($"There exists no image with ID {imageId}");
        }
    }
}