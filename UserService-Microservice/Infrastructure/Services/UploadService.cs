using System.IO;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UserService.Domain.Interfaces;

namespace UserService.Infrastructure.Services;

public class UploadService : IUploadService
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly string _bucketRegion;
    private readonly ILogger<UploadService> _logger;

    public UploadService(IAmazonS3 s3Client, IConfiguration configuration, ILogger<UploadService> logger)
    {
        _s3Client = s3Client;
        _logger = logger;
        _bucketName = configuration["AwsS3:BucketName"]
            ?? throw new ArgumentNullException("AwsS3:BucketName configuration is missing");
        _bucketRegion = configuration["AWS:Region"] ?? RegionEndpoint.USEast1.SystemName;

        if (string.IsNullOrWhiteSpace(_bucketName))
        {
            throw new ArgumentException("AwsS3:BucketName cannot be empty");
        }
    }

    public async Task<string> UploadFileAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        ValidateFile(file);

        var objectKey = $"avatars/{Guid.NewGuid():N}_{Path.GetFileName(file.FileName)}";
        _logger.LogInformation("Uploading profile image {Key} to S3 bucket {Bucket}", objectKey, _bucketName);

        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);
        stream.Position = 0;

        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = objectKey,
            InputStream = stream,
            ContentType = file.ContentType,
            CannedACL = S3CannedACL.PublicRead
        };

        await _s3Client.PutObjectAsync(request, cancellationToken);
        return BuildPublicUrl(objectKey);
    }

    public async Task DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
        {
            return;
        }

        var key = ExtractObjectKey(fileUrl);
        if (string.IsNullOrEmpty(key))
        {
            _logger.LogWarning("Unable to determine S3 object key from URL {Url}", fileUrl);
            return;
        }

        try
        {
            _logger.LogInformation("Deleting profile image {Key} from S3 bucket {Bucket}", key, _bucketName);
            var request = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = key
            };

            await _s3Client.DeleteObjectAsync(request, cancellationToken);
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete profile image {Key}", key);
        }
    }

    private static void ValidateFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File is empty");
        }

        if (file.Length > MaxFileSizeBytes)
        {
            throw new ArgumentException("File exceeds 5MB limit");
        }

        if (!AllowedContentTypes.Contains(file.ContentType))
        {
            throw new ArgumentException("Unsupported image type");
        }
    }

    private string BuildPublicUrl(string key)
    {
        return $"https://{_bucketName}.s3.{_bucketRegion}.amazonaws.com/{key}";
    }

    private static string? ExtractObjectKey(string fileUrl)
    {
        if (Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri))
        {
            var key = uri.AbsolutePath.TrimStart('/');
            return string.IsNullOrWhiteSpace(key) ? null : key;
        }

        return Path.GetFileName(fileUrl);
    }
}
