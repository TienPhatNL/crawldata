namespace ClassroomService.Domain.Constants;

/// <summary>
/// Constants for template management
/// </summary>
public static class TemplateConstants
{
    /// <summary>
    /// Maximum file size allowed for template uploads (5MB)
    /// </summary>
    public const long MaxFileSizeBytes = 5 * 1024 * 1024;

    /// <summary>
    /// MIME types for file operations
    /// </summary>
    public static class MimeTypes
    {
        public const string WordDocument = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
        public const string Html = "text/html";
        public const string Json = "application/json";
    }

    /// <summary>
    /// Allowed file extensions for template uploads
    /// </summary>
    public static class AllowedExtensions
    {
        public const string Docx = ".docx";
    }
}
