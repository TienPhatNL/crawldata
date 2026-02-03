using Microsoft.AspNetCore.Http;

namespace ClassroomService.Application.Common.Helpers;

/// <summary>
/// Helper class for validating uploaded files
/// </summary>
public static class FileValidationHelper
{
    // Allowed file extensions
    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private static readonly string[] AllowedReportFileExtensions = { ".pdf", ".doc", ".docx", ".txt", ".zip", ".rar" };
    private static readonly string[] AllowedAssignmentFileExtensions = { ".pdf", ".doc", ".docx", ".txt", ".zip", ".rar", ".ppt", ".pptx", ".xls", ".xlsx" };
    private static readonly string[] AllowedCsvExtensions = { ".csv" };
    
    // File size limits (in bytes)
    private const long MaxImageSize = 50 * 1024 * 1024; // 50MB
    private const long MaxReportFileSize = 50 * 1024 * 1024; // 50MB
    private const long MaxAssignmentFileSize = 50 * 1024 * 1024; // 50MB
    private const long MaxCsvFileSize = 10 * 1024 * 1024; // 10MB

    /// <summary>
    /// Validates an image file for course upload
    /// </summary>
    /// <param name="file">The uploaded file</param>
    /// <param name="errorMessage">Output error message if validation fails</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool ValidateImageFile(IFormFile file, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (file == null || file.Length == 0)
        {
            errorMessage = "File is empty or not provided";
            return false;
        }

        // Check file size
        if (file.Length > MaxImageSize)
        {
            errorMessage = $"File size exceeds maximum allowed size of {MaxImageSize / 1024 / 1024}MB";
            return false;
        }

        // Check file extension
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedImageExtensions.Contains(extension))
        {
            errorMessage = $"File type '{extension}' is not allowed. Allowed types: {string.Join(", ", AllowedImageExtensions)}";
            return false;
        }

        // Validate content type
        if (!IsValidImageContentType(file.ContentType))
        {
            errorMessage = "Invalid image content type";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates a report file for submission
    /// </summary>
    /// <param name="file">The uploaded file</param>
    /// <param name="errorMessage">Output error message if validation fails</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool ValidateReportFile(IFormFile file, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (file == null || file.Length == 0)
        {
            errorMessage = "File is empty or not provided";
            return false;
        }

        // Check file size
        if (file.Length > MaxReportFileSize)
        {
            errorMessage = $"File size exceeds maximum allowed size of {MaxReportFileSize / 1024 / 1024}MB";
            return false;
        }

        // Check file extension
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedReportFileExtensions.Contains(extension))
        {
            errorMessage = $"File type '{extension}' is not allowed. Allowed types: {string.Join(", ", AllowedReportFileExtensions)}";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates an assignment attachment file
    /// </summary>
    /// <param name="file">The uploaded file</param>
    /// <param name="errorMessage">Output error message if validation fails</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool ValidateAssignmentFile(IFormFile file, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (file == null || file.Length == 0)
        {
            errorMessage = "File is empty or not provided";
            return false;
        }

        // Check file size
        if (file.Length > MaxAssignmentFileSize)
        {
            errorMessage = $"File size exceeds maximum allowed size of {MaxAssignmentFileSize / 1024 / 1024}MB";
            return false;
        }

        // Check file extension
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedAssignmentFileExtensions.Contains(extension))
        {
            errorMessage = $"File type '{extension}' is not allowed. Allowed types: {string.Join(", ", AllowedAssignmentFileExtensions)}";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Gets the appropriate content type based on file extension
    /// </summary>
    public static string GetContentType(string fileExtension)
    {
        return fileExtension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".txt" => "text/plain",
            ".zip" => "application/zip",
            ".rar" => "application/x-rar-compressed",
            _ => "application/octet-stream"
        };
    }

    /// <summary>
    /// Checks if the content type is a valid image type
    /// </summary>
    private static bool IsValidImageContentType(string contentType)
    {
        var validTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        return validTypes.Contains(contentType.ToLowerInvariant());
    }

    /// <summary>
    /// Checks if a file extension is an allowed image type
    /// </summary>
    public static bool IsAllowedImageExtension(string extension)
    {
        return AllowedImageExtensions.Contains(extension.ToLowerInvariant());
    }

    /// <summary>
    /// Checks if a file extension is an allowed report file type
    /// </summary>
    public static bool IsAllowedReportFileExtension(string extension)
    {
        return AllowedReportFileExtensions.Contains(extension.ToLowerInvariant());
    }

    /// <summary>
    /// Validates a CSV file for conversation upload
    /// </summary>
    /// <param name="file">The uploaded file</param>
    /// <param name="errorMessage">Output error message if validation fails</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool ValidateCsvFile(IFormFile file, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (file == null || file.Length == 0)
        {
            errorMessage = "File is empty or not provided";
            return false;
        }

        // Check file size
        if (file.Length > MaxCsvFileSize)
        {
            errorMessage = $"File size exceeds maximum allowed size of {MaxCsvFileSize / 1024 / 1024}MB";
            return false;
        }

        // Check file extension
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedCsvExtensions.Contains(extension))
        {
            errorMessage = $"File type '{extension}' is not allowed. Only CSV files (.csv) are supported.";
            return false;
        }

        return true;
    }
}
