using ClassroomService.Application.Common.Helpers;
using ClassroomService.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClassroomService.Application.Controllers;

/// <summary>
/// Controller for anonymous image uploads (for report collaboration)
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[AllowAnonymous]
public class ImageUploadController : ControllerBase
{
    private readonly IUploadService _uploadService;
    private readonly ILogger<ImageUploadController> _logger;

    public ImageUploadController(
        IUploadService uploadService,
        ILogger<ImageUploadController> logger)
    {
        _uploadService = uploadService;
        _logger = logger;
    }

    /// <summary>
    /// Upload a single image (anonymous access for report collaboration)
    /// </summary>
    /// <param name="image">Image file to upload (max 100MB)</param>
    /// <returns>S3 URL of uploaded image</returns>
    /// <response code="200">Image uploaded successfully</response>
    /// <response code="400">Invalid file or validation error</response>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(ImageUploadResponse), 200)]
    [ProducesResponseType(400)]
    [RequestFormLimits(MultipartBodyLengthLimit = 104857600)] // 100MB
    [RequestSizeLimit(104857600)] // 100MB
    public async Task<ActionResult<ImageUploadResponse>> UploadImage(IFormFile image)
    {
        try
        {
            // Validate file exists
            if (image == null || image.Length == 0)
            {
                return BadRequest(new ImageUploadResponse
                {
                    Success = false,
                    Message = "No file uploaded"
                });
            }

            // Validate image file
            if (!FileValidationHelper.ValidateImageFile(image, out var validationError))
            {
                return BadRequest(new ImageUploadResponse
                {
                    Success = false,
                    Message = validationError
                });
            }

            // Validate size (100MB max)
            const long maxSizeBytes = 104857600; // 100MB
            if (image.Length > maxSizeBytes)
            {
                return BadRequest(new ImageUploadResponse
                {
                    Success = false,
                    Message = $"File size exceeds maximum limit of {maxSizeBytes / 1024 / 1024}MB"
                });
            }

            // Upload to S3
            var imageUrl = await _uploadService.UploadFileAsync(image);

            _logger.LogInformation(
                "Anonymous image uploaded successfully. Size: {Size}KB, URL: {Url}",
                image.Length / 1024,
                imageUrl);

            return Ok(new ImageUploadResponse
            {
                Success = true,
                Message = "Image uploaded successfully",
                ImageUrl = imageUrl,
                FileName = image.FileName,
                FileSizeBytes = image.Length,
                ContentType = image.ContentType
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image. FileName: {FileName}, Size: {Size}",
                image?.FileName, image?.Length);
            
            return BadRequest(new ImageUploadResponse
            {
                Success = false,
                Message = $"An error occurred while uploading the image: {ex.Message}"
            });
        }
    }
}

/// <summary>
/// Response for image upload
/// </summary>
public class ImageUploadResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? FileName { get; set; }
    public long? FileSizeBytes { get; set; }
    public string? ContentType { get; set; }
}
