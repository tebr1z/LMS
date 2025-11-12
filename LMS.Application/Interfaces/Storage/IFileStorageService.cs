namespace LMS.Application.Interfaces.Storage;

/// <summary>
/// Interface for file storage operations (supports AWS S3, local storage, etc.)
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Uploads a file and returns the file URL/path
    /// </summary>
    /// <param name="fileStream">File stream to upload</param>
    /// <param name="fileName">Original file name</param>
    /// <param name="contentType">File content type (MIME type)</param>
    /// <param name="folder">Optional folder/path prefix</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File URL or path</returns>
    Task<string> UploadFileAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        string? folder = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file by its URL/path
    /// </summary>
    /// <param name="fileUrl">File URL or path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deletion was successful</returns>
    Task<bool> DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a file stream by its URL/path
    /// </summary>
    /// <param name="fileUrl">File URL or path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File stream</returns>
    Task<Stream> GetFileAsync(string fileUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a file exists
    /// </summary>
    /// <param name="fileUrl">File URL or path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if file exists</returns>
    Task<bool> FileExistsAsync(string fileUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a temporary URL for file access (useful for S3 pre-signed URLs)
    /// </summary>
    /// <param name="fileUrl">File URL or path</param>
    /// <param name="expirationMinutes">URL expiration time in minutes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Temporary access URL</returns>
    Task<string> GetTemporaryUrlAsync(
        string fileUrl,
        int expirationMinutes = 60,
        CancellationToken cancellationToken = default);
}

