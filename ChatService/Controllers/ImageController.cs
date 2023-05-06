using System.Drawing;
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
    private readonly ILogger<ImageController> _logger;

    public ImageController(IImageService imageService, ILogger<ImageController> logger)
    {
        _imageService = imageService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<UploadImageResponse>> UploadImage([FromForm] UploadImageRequest uploadImageRequest)
    {
        try
        {
            var profilePictureData = uploadImageRequest.File.OpenReadStream();
            var imageId = await _imageService.UploadImage(profilePictureData);
            _logger.LogInformation("Image uploaded successfully. Image ID: {ImageId}", imageId);
            return Ok(new UploadImageResponse(imageId));
        }
        catch (ArgumentException)
        {
            _logger.LogWarning("Image file is empty");
            return BadRequest("The Image file is empty");
        }
        catch
        {
            _logger.LogError("There was an error uploading the image");
            throw;
        }
    }

    [HttpGet("{imageId}")]
    public async Task<ActionResult> DownloadImage([FromRoute] string imageId)
    {
        using (_logger.BeginScope("{ImageId}", imageId))
        {
            try
            {
                var imageData = await _imageService.DownloadImage(imageId);
                var imageMemoryStream = new MemoryStream();
                await imageData.CopyToAsync(imageMemoryStream);
                imageMemoryStream.Position = 0;
                _logger.LogInformation("Image downloaded successfully. Image Id: {ImageId}", imageId);
                return new FileContentResult(imageMemoryStream.ToArray(), "image/jpeg");
            }
            catch (ArgumentException)
            {
                _logger.LogWarning("Invalid Argument: Image ID cannot be null or empty");
                return BadRequest("Invalid Argument: Image ID cannot be null or empty");
            }
            catch (ImageNotFoundException)
            {
                _logger.LogWarning("Image not found. Image Id: {imageId}", imageId);
                return NotFound($"There exists no image with ID {imageId}");
            }
            catch
            {
                _logger.LogError("There was an error downloading the image. Image Id: {ImageId}", imageId);
                throw;
            }
        }
    }
}