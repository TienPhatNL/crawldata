using WebCrawlerService.Domain.Models;

namespace WebCrawlerService.Domain.Interfaces;

/// <summary>
/// Client for communicating with Android MCP server
/// </summary>
public interface IMcpClientService : IDisposable
{
    /// <summary>
    /// Connect to Android device
    /// </summary>
    Task<bool> ConnectAsync(string? deviceName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnect from Android device
    /// </summary>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Open an app by package name or deep link
    /// </summary>
    Task<bool> OpenAppAsync(string identifier, bool isDeepLink = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tap an element at coordinates or by resource ID
    /// </summary>
    Task<bool> TapAsync(int? x = null, int? y = null, string? resourceId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Input text into a field
    /// </summary>
    Task<bool> InputTextAsync(string text, string? resourceId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Scroll in specified direction
    /// </summary>
    Task<bool> ScrollAsync(string direction = "down", int distance = 500, CancellationToken cancellationToken = default);

    /// <summary>
    /// Swipe gesture
    /// </summary>
    Task<bool> SwipeAsync(int startX, int startY, int endX, int endY, int duration = 500, CancellationToken cancellationToken = default);

    /// <summary>
    /// Press back button
    /// </summary>
    Task<bool> BackAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Capture screenshot
    /// </summary>
    Task<string> GetScreenshotAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get UI hierarchy XML
    /// </summary>
    Task<string> GetUiHierarchyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get complete screen state (screenshot + hierarchy + text)
    /// </summary>
    Task<ScreenState> GetScreenStateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if MCP server is running and responsive
    /// </summary>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}
