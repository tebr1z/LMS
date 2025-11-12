using LMS.Application.Interfaces.Storage;
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
    private readonly ILogger<FilesController> _logger;

    // Allowed file extensions
    private static readonly string[] AllowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png", ".mp4", ".docx" };
    private const long MaxFileSize = 100_000_000; // 100 MB

    public FilesController(IFileStorageService fileStorageService, ILogger<FilesController> logger)
    {
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    /// <summary>
    /// Upload a file and get the FileUrl
    /// Allowed file types: PDF, JPG, PNG, MP4, DOCX
    /// Maximum file size: 100 MB
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

            // Validate file extension
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(fileExtension))
            {
                return BadRequest(new { 
                    message = $"Invalid file type. Allowed types: PDF, JPG, PNG, MP4, DOCX. Received: {fileExtension}" 
                });
            }

            // Validate file size
            if (file.Length > MaxFileSize)
            {
                return BadRequest(new { 
                    message = $"File size exceeds maximum limit of {MaxFileSize / 1_000_000} MB." 
                });
            }

            // Upload file
            using var fileStream = file.OpenReadStream();
            var fileUrl = await _fileStorageService.UploadFileAsync(
                fileStream,
                file.FileName,
                file.ContentType,
                folder: folder,
                cancellationToken);

            _logger.LogInformation("File uploaded successfully: {FileName} -> {FileUrl}", file.FileName, fileUrl);

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

            var deleted = await _fileStorageService.DeleteFileAsync(fileUrl, cancellationToken);
            
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

