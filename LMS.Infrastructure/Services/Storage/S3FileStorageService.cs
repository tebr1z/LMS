using LMS.Application.Interfaces.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LMS.Infrastructure.Services.Storage;

/// <summary>
/// AWS S3 file storage implementation
/// To use this, install: AWSSDK.S3 NuGet package and configure AWS credentials
/// Configuration:
///   FileStorage:S3:BucketName - S3 bucket name (required)
///   FileStorage:S3:Region - AWS region (default: us-east-1)
///   FileStorage:S3:AccessKey - AWS access key (optional, can use IAM role)
///   FileStorage:S3:SecretKey - AWS secret key (optional, can use IAM role)
/// </summary>
public class S3FileStorageService : IFileStorageService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<S3FileStorageService> _logger;
    private readonly string _bucketName;
    private readonly string _region;
    private readonly string? _accessKey;
    private readonly string? _secretKey;

    public S3FileStorageService(IConfiguration configuration, ILogger<S3FileStorageService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _bucketName = configuration["FileStorage:S3:BucketName"] ?? throw new InvalidOperationException("S3 BucketName not configured");
        _region = configuration["FileStorage:S3:Region"] ?? "us-east-1";
        _accessKey = configuration["FileStorage:S3:AccessKey"];
        _secretKey = configuration["FileStorage:S3:SecretKey"];
    }

    public async Task<string> UploadAsync(
        IFormFile file,
        string? folder = null,
        CancellationToken cancellationToken = default)
    {
        using var fileStream = file.OpenReadStream();
        return await UploadFileAsync(
            fileStream,
            file.FileName,
            file.ContentType,
            folder,
            cancellationToken);
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
            // Check if AWS SDK is available
            var s3ClientType = Type.GetType("Amazon.S3.AmazonS3Client, AWSSDK.S3");
            if (s3ClientType == null)
            {
                throw new InvalidOperationException(
                    "AWS SDK for S3 is not installed. Install AWSSDK.S3 NuGet package to use S3 storage.");
            }

            // Create S3 client using reflection (to avoid compile-time dependency)
            object? s3Client;
            if (!string.IsNullOrEmpty(_accessKey) && !string.IsNullOrEmpty(_secretKey))
            {
                // Use access key and secret key
                var regionEndpointType = Type.GetType("Amazon.RegionEndpoint, AWSSDK.Core");
                var regionEndpoint = regionEndpointType?.GetProperty(_region)?.GetValue(null);
                
                var credentialsType = Type.GetType("Amazon.Runtime.BasicAWSCredentials, AWSSDK.Core");
                var credentials = Activator.CreateInstance(credentialsType!, _accessKey, _secretKey);
                
                s3Client = Activator.CreateInstance(s3ClientType, credentials, regionEndpoint);
            }
            else
            {
                // Use default credentials (IAM role, environment variables, etc.)
                s3Client = Activator.CreateInstance(s3ClientType);
            }

            // Sanitize file name
            var sanitizedFileName = SanitizeFileName(fileName);
            var uniqueFileName = $"{Guid.NewGuid()}_{sanitizedFileName}";
            var key = string.IsNullOrEmpty(folder) 
                ? uniqueFileName 
                : $"{folder}/{uniqueFileName}";

            // Create PutObjectRequest
            var putObjectRequestType = Type.GetType("Amazon.S3.Model.PutObjectRequest, AWSSDK.S3");
            var putObjectRequest = Activator.CreateInstance(putObjectRequestType!);
            
            putObjectRequestType!.GetProperty("BucketName")!.SetValue(putObjectRequest, _bucketName);
            putObjectRequestType.GetProperty("Key")!.SetValue(putObjectRequest, key);
            putObjectRequestType.GetProperty("InputStream")!.SetValue(putObjectRequest, fileStream);
            putObjectRequestType.GetProperty("ContentType")!.SetValue(putObjectRequest, contentType);

            // Upload file
            var putObjectMethod = s3ClientType.GetMethod("PutObjectAsync", new[] { putObjectRequestType, typeof(CancellationToken) });
            var task = putObjectMethod!.Invoke(s3Client, new[] { putObjectRequest, cancellationToken }) as Task;
            await task!;

            // Return S3 URL
            var fileUrl = $"https://{_bucketName}.s3.{_region}.amazonaws.com/{key}";
            _logger.LogInformation("File uploaded to S3: {FileName} -> {FileUrl}", fileName, fileUrl);
            
            return fileUrl;
        }
        catch (InvalidOperationException)
        {
            throw; // Re-throw configuration errors
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to S3: {FileName}", fileName);
            throw new InvalidOperationException($"Failed to upload file to S3. Ensure AWS SDK is installed and configured correctly.", ex);
        }
    }

    public async Task<bool> DeleteAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        return await DeleteFileAsync(fileUrl, cancellationToken);
    }

    public async Task<bool> DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var s3ClientType = Type.GetType("Amazon.S3.AmazonS3Client, AWSSDK.S3");
            if (s3ClientType == null)
            {
                throw new InvalidOperationException("AWS SDK for S3 is not installed.");
            }

            // Extract key from URL
            var key = ExtractKeyFromUrl(fileUrl);
            
            // Create S3 client
            var s3Client = Activator.CreateInstance(s3ClientType);

            // Create DeleteObjectRequest
            var deleteObjectRequestType = Type.GetType("Amazon.S3.Model.DeleteObjectRequest, AWSSDK.S3");
            var deleteObjectRequest = Activator.CreateInstance(deleteObjectRequestType!);
            
            deleteObjectRequestType!.GetProperty("BucketName")!.SetValue(deleteObjectRequest, _bucketName);
            deleteObjectRequestType.GetProperty("Key")!.SetValue(deleteObjectRequest, key);

            // Delete file
            var deleteObjectMethod = s3ClientType.GetMethod("DeleteObjectAsync", new[] { deleteObjectRequestType, typeof(CancellationToken) });
            var task = deleteObjectMethod!.Invoke(s3Client, new[] { deleteObjectRequest, cancellationToken }) as Task;
            await task!;

            _logger.LogInformation("File deleted from S3: {FileUrl}", fileUrl);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file from S3: {FileUrl}", fileUrl);
            return false;
        }
    }

    public async Task<Stream> GetFileAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var s3ClientType = Type.GetType("Amazon.S3.AmazonS3Client, AWSSDK.S3");
            if (s3ClientType == null)
            {
                throw new InvalidOperationException("AWS SDK for S3 is not installed.");
            }

            // Extract key from URL
            var key = ExtractKeyFromUrl(fileUrl);
            
            // Create S3 client
            var s3Client = Activator.CreateInstance(s3ClientType);

            // Create GetObjectRequest
            var getObjectRequestType = Type.GetType("Amazon.S3.Model.GetObjectRequest, AWSSDK.S3");
            var getObjectRequest = Activator.CreateInstance(getObjectRequestType!);
            
            getObjectRequestType!.GetProperty("BucketName")!.SetValue(getObjectRequest, _bucketName);
            getObjectRequestType.GetProperty("Key")!.SetValue(getObjectRequest, key);

            // Get file
            var getObjectMethod = s3ClientType.GetMethod("GetObjectAsync", new[] { getObjectRequestType, typeof(CancellationToken) });
            var task = getObjectMethod!.Invoke(s3Client, new[] { getObjectRequest, cancellationToken }) as Task;
            await task!;

            // Extract ResponseStream from response
            var responseType = task.GetType().GetGenericArguments()[0];
            var response = task.GetType().GetProperty("Result")!.GetValue(task);
            var responseStream = responseType.GetProperty("ResponseStream")!.GetValue(response) as Stream;

            return responseStream ?? throw new FileNotFoundException($"File not found: {fileUrl}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file from S3: {FileUrl}", fileUrl);
            throw new FileNotFoundException($"File not found: {fileUrl}", ex);
        }
    }

    public async Task<bool> FileExistsAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            await GetFileAsync(fileUrl, cancellationToken);
            return true;
        }
        catch (FileNotFoundException)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string> GetTemporaryUrlAsync(
        string fileUrl,
        int expirationMinutes = 60,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var s3ClientType = Type.GetType("Amazon.S3.AmazonS3Client, AWSSDK.S3");
            if (s3ClientType == null)
            {
                throw new InvalidOperationException("AWS SDK for S3 is not installed.");
            }

            // Extract key from URL
            var key = ExtractKeyFromUrl(fileUrl);
            
            // Create S3 client
            var s3Client = Activator.CreateInstance(s3ClientType);

            // Create GetPreSignedUrlRequest
            var getPreSignedUrlRequestType = Type.GetType("Amazon.S3.Model.GetPreSignedUrlRequest, AWSSDK.S3");
            var getPreSignedUrlRequest = Activator.CreateInstance(getPreSignedUrlRequestType!);
            
            getPreSignedUrlRequestType!.GetProperty("BucketName")!.SetValue(getPreSignedUrlRequest, _bucketName);
            getPreSignedUrlRequestType.GetProperty("Key")!.SetValue(getPreSignedUrlRequest, key);
            getPreSignedUrlRequestType.GetProperty("Expires")!.SetValue(getPreSignedUrlRequest, DateTime.UtcNow.AddMinutes(expirationMinutes));
            getPreSignedUrlRequestType.GetProperty("Verb")!.SetValue(getPreSignedUrlRequest, 
                Enum.Parse(Type.GetType("Amazon.S3.HttpVerb, AWSSDK.S3")!, "GET"));

            // Get pre-signed URL
            var getPreSignedURLMethod = s3ClientType.GetMethod("GetPreSignedURL", new[] { getPreSignedUrlRequestType });
            var preSignedUrl = getPreSignedURLMethod!.Invoke(s3Client, new[] { getPreSignedUrlRequest }) as string;

            return preSignedUrl ?? fileUrl;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error generating pre-signed URL for S3, returning original URL: {FileUrl}", fileUrl);
            return fileUrl; // Fallback to original URL
        }
    }

    private string ExtractKeyFromUrl(string fileUrl)
    {
        // Extract key from S3 URL: https://bucket.s3.region.amazonaws.com/folder/key
        var uri = new Uri(fileUrl);
        return uri.AbsolutePath.TrimStart('/');
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
    }
}

