using ChatService.Dtos;
using ChatService.Exceptions;
using ChatService.Services;
using Microsoft.AspNetCore.Mvc;


namespace ChatService.Controllers;

[ApiController]
[Route("api/images")]
public class ImageController : ControllerBase
{
    private readonly IImageService _imageService;

    public ImageController(IImageService imageService)
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
            return BadRequest("The Image file is empty");
        }
    }

    [HttpGet("{imageId}")]
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