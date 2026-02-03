namespace WebCrawlerService.Infrastructure.Common;

/// <summary>
/// Configuration settings for MCP (Model Context Protocol) client
/// </summary>
public class McpSettings
{
    public const string SectionName = "McpSettings";

    /// <summary>
    /// Path to Python executable
    /// </summary>
    public string PythonPath { get; init; } = "python3";

    /// <summary>
    /// Path to MCP server.py file
    /// </summary>
    public string ServerPath { get; init; } = "/root/projects/crawldata/MCP-Servers/android-mcp-server/server.py";

    /// <summary>
    /// Appium server URL
    /// </summary>
    public string AppiumServerUrl { get; init; } = "http://localhost:4723";

    /// <summary>
    /// Default Android device name
    /// </summary>
    public string DeviceName { get; init; } = "emulator-5554";

    /// <summary>
    /// Timeout for MCP requests in seconds
    /// </summary>
    public int RequestTimeoutSeconds { get; init; } = 30;

    /// <summary>
    /// Enable automatic reconnection on connection loss
    /// </summary>
    public bool AutoReconnect { get; init; } = true;

    /// <summary>
    /// Maximum reconnection attempts
    /// </summary>
    public int MaxReconnectAttempts { get; init; } = 3;
}
