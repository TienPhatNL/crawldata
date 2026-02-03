namespace ClassroomService.Domain.Common;

/// <summary>
/// Configuration settings for AI content detection service
/// </summary>
public class AIDetectionSettings
{
    /// <summary>
    /// AI detection provider name (e.g., "ZeroGPT")
    /// </summary>
    public string Provider { get; set; } = string.Empty;
    
    /// <summary>
    /// API key/token for the AI detection service
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
    
    /// <summary>
    /// API endpoint URL
    /// </summary>
    public string ApiEndpoint { get; set; } = string.Empty;
    
    /// <summary>
    /// Model name or identifier
    /// </summary>
    public string ModelName { get; set; } = string.Empty;
    
    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    public int Timeout { get; set; } = 30;
    
    /// <summary>
    /// Maximum content length to analyze (in characters)
    /// </summary>
    public int MaxContentLength { get; set; } = 50000;
}
