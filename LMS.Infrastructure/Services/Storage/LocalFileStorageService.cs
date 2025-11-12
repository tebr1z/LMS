using LMS.Application.Interfaces.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LMS.Infrastructure.Services.Storage;

/// <summary>
/// Local file storage implementation (stores files on the server's file system)
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(IConfiguration configuration, ILogger<LocalFileStorageService> logger)
    {
        _basePath = configuration["FileStorage:LocalPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        _logger = logger;

        // Ensure base directory exists
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    public async Task<string> UploadFileAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        string? folder = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sanitizedFileName = SanitizeFileName(fileName);
            var uniqueFileName = $"{Guid.NewGuid()}_{sanitizedFileName}";
            var filePath = string.IsNullOrEmpty(folder)
                ? Path.Combine(_basePath, uniqueFileName)
                : Path.Combine(_basePath, folder, uniqueFileName);

            // Ensure folder exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var fileStreamWriter = new FileStream(filePath, FileMode.Create);
            await fileStream.CopyToAsync(fileStreamWriter, cancellationToken);

            var relativePath = Path.GetRelativePath(_basePath, filePath).Replace('\\', '/');
            return $"/uploads/{relativePath}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file: {FileName}", fileName);
            throw;
        }
    }

    public async Task<bool> DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var filePath = GetFilePath(fileUrl);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FileUrl}", fileUrl);
            return false;
        }
    }

    public async Task<Stream> GetFileAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        var filePath = GetFilePath(fileUrl);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {fileUrl}");
        }

        return new FileStream(filePath, FileMode.Open, FileAccess.Read);
    }

    public async Task<bool> FileExistsAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        var filePath = GetFilePath(fileUrl);
        return File.Exists(filePath);
    }

    public async Task<string> GetTemporaryUrlAsync(
        string fileUrl,
        int expirationMinutes = 60,
        CancellationToken cancellationToken = default)
    {
        // For local storage, return the file URL directly
        // In production, you might want to generate a token-based URL
        return fileUrl;
    }

    private string GetFilePath(string fileUrl)
    {
        // Remove leading slash and "uploads/" prefix if present
        var relativePath = fileUrl.TrimStart('/').Replace("uploads/", "");
        return Path.Combine(_basePath, relativePath);
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
    }
}

