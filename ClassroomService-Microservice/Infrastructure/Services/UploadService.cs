using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ClassroomService.Domain.Interfaces;

namespace ClassroomService.Infrastructure.Services;

public class UploadService : IUploadService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly ILogger<UploadService> _logger;

    public UploadService(
        IAmazonS3 s3Client,
        IConfiguration configuration,
        ILogger<UploadService> logger)
    {
        _s3Client = s3Client;
        _logger = logger;
        _bucketName = configuration["AwsS3:BucketName"]
            ?? throw new ArgumentNullException("AWS S3 BucketName configuration is missing");

        if (string.IsNullOrWhiteSpace(_bucketName))
        {
            throw new ArgumentException("AWS S3 BucketName cannot be empty");
        }
    }

    public async Task<string> UploadFileAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("Upload attempt with empty file");
            throw new ArgumentException("File is empty. Please provide a valid file.");
        }

        if (file.Length > 10 * 1024 * 1024) // 10MB limit
        {
            _logger.LogWarning("Upload attempt with file size {Size}MB exceeding 10MB limit", file.Length / 1024.0 / 1024.0);
            throw new ArgumentException("File size exceeds 10MB limit.");
        }

        try
        {
            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            _logger.LogInformation("Uploading file {FileName} to S3 bucket {BucketName}", fileName, _bucketName);

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                stream.Position = 0;

                var request = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = fileName,
                    InputStream = stream,
                    ContentType = file.ContentType,
                    CannedACL = S3CannedACL.PublicRead
                };

                await _s3Client.PutObjectAsync(request);
                var fileUrl = $"https://{_bucketName}.s3.amazonaws.com/{fileName}";

                _logger.LogInformation("File uploaded successfully: {FileUrl}", fileUrl);
                return fileUrl;
            }
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "S3 error while uploading file");
            throw new Exception($"S3 Error: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while uploading file");
            throw;
        }
    }

    public async Task DeleteFileAsync(string fileUrl)
    {
        try
        {
            var key = Path.GetFileName(fileUrl);
            _logger.LogInformation("Deleting file {Key} from S3 bucket {BucketName}", key, _bucketName);

            var request = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = key
            };

            await _s3Client.DeleteObjectAsync(request);
            _logger.LogInformation("File deleted successfully: {Key}", key);
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "S3 error while deleting file {FileUrl}", fileUrl);
            throw new Exception($"S3 Error: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while deleting file {FileUrl}", fileUrl);
            throw;
        }
    }
}
