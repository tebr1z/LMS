using LMS.Application.Interfaces;
using LMS.Application.Interfaces.Storage;
using LMS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly IFileStorageService _fileStorageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<FilesController> _logger;

    // Allowed file extensions: pdf, docx, jpg, jpeg, png, mp4
    private static readonly string[] AllowedExtensions = { ".pdf", ".docx", ".jpg", ".jpeg", ".png", ".mp4" };
    private const long MaxFileSize = 50_000_000; // 50 MB

    public FilesController(
        IFileStorageService fileStorageService,
        IUnitOfWork unitOfWork,
        ILogger<FilesController> logger)
    {
        _fileStorageService = fileStorageService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Upload a file and get the FileUrl
    /// Validates extensions: pdf, docx, jpg, jpeg, png, mp4
    /// Maximum file size: 50 MB
    /// Stores File metadata in File table
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(MaxFileSize)]
    public async Task<ActionResult<FileUploadResponse>> UploadFile(
        [FromForm] IFormFile file,
        [FromQuery] string? folder = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate file is provided
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file provided or file is empty." });
            }

            // Validate file extension: pdf, docx, jpg, jpeg, png, mp4
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(fileExtension))
            {
                return BadRequest(new { 
                    message = $"Invalid file type. Allowed types: PDF, DOCX, JPG, JPEG, PNG, MP4. Received: {fileExtension}" 
                });
            }

            // Validate file size: Max size: 50 MB
            if (file.Length > MaxFileSize)
            {
                return BadRequest(new { 
                    message = $"File size exceeds maximum limit of {MaxFileSize / 1_000_000} MB." 
                });
            }

            // Get current user ID from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var uploadedById))
            {
                return Unauthorized(new { message = "Invalid user token." });
            }

            // Upload file using IFormFile overload
            var fileUrl = await _fileStorageService.UploadAsync(
                file,
                folder: folder,
                cancellationToken);

            // Store File metadata in File table
            var fileEntity = new File
            {
                Url = fileUrl,
                FileName = file.FileName,
                ContentType = file.ContentType,
                Size = file.Length,
                UploadedById = uploadedById,
                UploadedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Files.AddAsync(fileEntity);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("File uploaded successfully: {FileName} -> {FileUrl} (FileId: {FileId})", 
                file.FileName, fileUrl, fileEntity.Id);

            return Ok(new FileUploadResponse
            {
                FileUrl = fileUrl,
                FileName = file.FileName,
                FileSize = file.Length,
                ContentType = file.ContentType
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file: {FileName}", file?.FileName);
            return StatusCode(500, new { message = "An error occurred while uploading the file.", error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a file by its URL
    /// </summary>
    [HttpDelete("{*fileUrl}")]
    [Authorize(Roles = "Teacher,Admin,MasterAdmin")]
    public async Task<ActionResult> DeleteFile(
        string fileUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
            {
                return BadRequest(new { message = "File URL is required." });
            }

            var deleted = await _fileStorageService.DeleteAsync(fileUrl, cancellationToken);
            
            if (deleted)
            {
                _logger.LogInformation("File deleted successfully: {FileUrl}", fileUrl);
                return Ok(new { message = "File deleted successfully.", fileUrl });
            }
            else
            {
                return NotFound(new { message = "File not found or could not be deleted.", fileUrl });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FileUrl}", fileUrl);
            return StatusCode(500, new { message = "An error occurred while deleting the file.", error = ex.Message });
        }
    }

    /// <summary>
    /// Check if a file exists
    /// </summary>
    [HttpGet("exists")]
    public async Task<ActionResult<FileExistsResponse>> FileExists(
        [FromQuery] string fileUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
            {
                return BadRequest(new { message = "File URL is required." });
            }

            var exists = await _fileStorageService.FileExistsAsync(fileUrl, cancellationToken);
            
            return Ok(new FileExistsResponse
            {
                FileUrl = fileUrl,
                Exists = exists
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking file existence: {FileUrl}", fileUrl);
            return StatusCode(500, new { message = "An error occurred while checking file existence.", error = ex.Message });
        }
    }
}

// Response DTOs
public class FileUploadResponse
{
    public string FileUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
}

public class FileExistsResponse
{
    public string FileUrl { get; set; } = string.Empty;
    public bool Exists { get; set; }
}

