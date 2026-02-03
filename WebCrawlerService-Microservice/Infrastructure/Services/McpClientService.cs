using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebCrawlerService.Domain.Interfaces;
using WebCrawlerService.Domain.Models;
using WebCrawlerService.Infrastructure.Common;

namespace WebCrawlerService.Infrastructure.Services;

/// <summary>
/// Client for communicating with Android MCP server via stdio
/// </summary>
public class McpClientService : IMcpClientService
{
    private readonly McpSettings _settings;
    private readonly ILogger<McpClientService> _logger;
    private Process? _mcpProcess;
    private StreamWriter? _processInput;
    private StreamReader? _processOutput;
    private int _requestId = 0;
    private bool _disposed = false;

    public McpClientService(
        IOptions<McpSettings> settings,
        ILogger<McpClientService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<bool> ConnectAsync(string? deviceName = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // Start MCP server process
            await StartMcpServerAsync(cancellationToken);

            // Call android_connect tool
            var response = await CallToolAsync("android_connect", new
            {
                device_name = deviceName ?? _settings.DeviceName
            }, cancellationToken);

            return response != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Android device");
            return false;
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_mcpProcess != null && !_mcpProcess.HasExited)
            {
                await CallToolAsync("android_disconnect", new { }, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting from Android device");
        }
        finally
        {
            StopMcpServer();
        }
    }

    public async Task<bool> OpenAppAsync(string identifier, bool isDeepLink = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await CallToolAsync("android_open_app", new
            {
                identifier,
                is_deep_link = isDeepLink
            }, cancellationToken);

            return response != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open app {Identifier}", identifier);
            return false;
        }
    }

    public async Task<bool> TapAsync(int? x = null, int? y = null, string? resourceId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var args = new Dictionary<string, object?>();
            if (x.HasValue) args["x"] = x.Value;
            if (y.HasValue) args["y"] = y.Value;
            if (!string.IsNullOrEmpty(resourceId)) args["resource_id"] = resourceId;

            var response = await CallToolAsync("android_tap", args, cancellationToken);
            return response != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to tap");
            return false;
        }
    }

    public async Task<bool> InputTextAsync(string text, string? resourceId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var args = new Dictionary<string, object?> { ["text"] = text };
            if (!string.IsNullOrEmpty(resourceId)) args["resource_id"] = resourceId;

            var response = await CallToolAsync("android_input_text", args, cancellationToken);
            return response != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to input text");
            return false;
        }
    }

    public async Task<bool> ScrollAsync(string direction = "down", int distance = 500, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await CallToolAsync("android_scroll", new
            {
                direction,
                distance
            }, cancellationToken);

            return response != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scroll");
            return false;
        }
    }

    public async Task<bool> SwipeAsync(int startX, int startY, int endX, int endY, int duration = 500, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await CallToolAsync("android_swipe", new
            {
                start_x = startX,
                start_y = startY,
                end_x = endX,
                end_y = endY,
                duration
            }, cancellationToken);

            return response != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to swipe");
            return false;
        }
    }

    public async Task<bool> BackAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await CallToolAsync("android_back", new { }, cancellationToken);
            return response != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to press back");
            return false;
        }
    }

    public async Task<string> GetScreenshotAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await CallToolAsync("android_screenshot", new { }, cancellationToken);

            if (response?.RootElement.TryGetProperty("content", out var content) == true &&
                content.GetArrayLength() > 0)
            {
                var firstContent = content[0];
                if (firstContent.TryGetProperty("type", out var type) &&
                    type.GetString() == "image" &&
                    firstContent.TryGetProperty("data", out var data))
                {
                    return data.GetString() ?? string.Empty;
                }
            }

            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get screenshot");
            return string.Empty;
        }
    }

    public async Task<string> GetUiHierarchyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await CallToolAsync("android_get_ui_hierarchy", new { }, cancellationToken);
            return ExtractTextContent(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get UI hierarchy");
            return string.Empty;
        }
    }

    public async Task<ScreenState> GetScreenStateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await CallToolAsync("android_get_screen_state", new { }, cancellationToken);
            var content = ExtractTextContent(response);

            if (string.IsNullOrEmpty(content))
            {
                return new ScreenState();
            }

            var stateJson = JsonDocument.Parse(content);
            var root = stateJson.RootElement;

            return new ScreenState
            {
                Screenshot = root.GetProperty("screenshot").GetString() ?? string.Empty,
                UiHierarchy = root.GetProperty("ui_hierarchy").GetString() ?? string.Empty,
                VisibleText = root.GetProperty("visible_text")
                    .EnumerateArray()
                    .Select(e => e.GetString() ?? string.Empty)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList(),
                Timestamp = DateTime.Parse(root.GetProperty("timestamp").GetString()!),
                Metadata = root.TryGetProperty("metadata", out var metadata)
                    ? metadata.EnumerateObject()
                        .ToDictionary(p => p.Name, p => p.Value.GetString() ?? string.Empty)
                    : new Dictionary<string, string>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get screen state");
            return new ScreenState();
        }
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_mcpProcess == null || _mcpProcess.HasExited)
            {
                return false;
            }

            // Try to list tools as a health check
            var response = await SendRequestAsync(new
            {
                jsonrpc = "2.0",
                id = GetNextRequestId(),
                method = "tools/list"
            }, cancellationToken);

            return response != null;
        }
        catch
        {
            return false;
        }
    }

    private async Task StartMcpServerAsync(CancellationToken cancellationToken)
    {
        if (_mcpProcess != null && !_mcpProcess.HasExited)
        {
            _logger.LogDebug("MCP server already running");
            return;
        }

        _logger.LogInformation("Starting MCP server from {Path}", _settings.ServerPath);

        var startInfo = new ProcessStartInfo
        {
            FileName = _settings.PythonPath,
            Arguments = $"\"{_settings.ServerPath}\"",
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(_settings.ServerPath)
        };

        // Set environment variables
        if (!string.IsNullOrEmpty(_settings.AppiumServerUrl))
        {
            startInfo.EnvironmentVariables["APPIUM_SERVER"] = _settings.AppiumServerUrl;
        }

        _mcpProcess = Process.Start(startInfo);

        if (_mcpProcess == null)
        {
            throw new InvalidOperationException("Failed to start MCP server process");
        }

        _processInput = _mcpProcess.StandardInput;
        _processOutput = _mcpProcess.StandardOutput;

        // Log stderr in background
        _ = Task.Run(async () =>
        {
            while (!_mcpProcess.HasExited)
            {
                var error = await _mcpProcess.StandardError.ReadLineAsync(cancellationToken);
                if (!string.IsNullOrEmpty(error))
                {
                    _logger.LogWarning("MCP server stderr: {Error}", error);
                }
            }
        }, cancellationToken);

        // Wait a bit for server to initialize
        await Task.Delay(1000, cancellationToken);

        _logger.LogInformation("MCP server started successfully");
    }

    private void StopMcpServer()
    {
        try
        {
            if (_mcpProcess != null && !_mcpProcess.HasExited)
            {
                _logger.LogInformation("Stopping MCP server");

                _processInput?.Close();
                _processOutput?.Close();

                if (!_mcpProcess.WaitForExit(5000))
                {
                    _mcpProcess.Kill();
                }

                _mcpProcess.Dispose();
                _mcpProcess = null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping MCP server");
        }
    }

    private async Task<JsonDocument?> CallToolAsync(
        string toolName,
        object arguments,
        CancellationToken cancellationToken)
    {
        var request = new
        {
            jsonrpc = "2.0",
            id = GetNextRequestId(),
            method = "tools/call",
            @params = new
            {
                name = toolName,
                arguments
            }
        };

        return await SendRequestAsync(request, cancellationToken);
    }

    private async Task<JsonDocument?> SendRequestAsync(object request, CancellationToken cancellationToken)
    {
        if (_processInput == null || _processOutput == null)
        {
            throw new InvalidOperationException("MCP server not started");
        }

        // Serialize and send request
        var requestJson = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        _logger.LogTrace("Sending MCP request: {Request}", requestJson);

        await _processInput.WriteLineAsync(requestJson.AsMemory(), cancellationToken);
        await _processInput.FlushAsync(cancellationToken);

        // Read response
        var responseLine = await _processOutput.ReadLineAsync(cancellationToken);

        if (string.IsNullOrEmpty(responseLine))
        {
            _logger.LogWarning("Empty response from MCP server");
            return null;
        }

        _logger.LogTrace("Received MCP response: {Response}", responseLine);

        var response = JsonDocument.Parse(responseLine);

        // Check for errors
        if (response.RootElement.TryGetProperty("error", out var error))
        {
            var errorMessage = error.GetProperty("message").GetString();
            _logger.LogError("MCP error: {Error}", errorMessage);
            throw new InvalidOperationException($"MCP error: {errorMessage}");
        }

        return response;
    }

    private string ExtractTextContent(JsonDocument? response)
    {
        if (response == null) return string.Empty;

        try
        {
            if (response.RootElement.TryGetProperty("result", out var result) &&
                result.TryGetProperty("content", out var content) &&
                content.GetArrayLength() > 0)
            {
                var firstContent = content[0];
                if (firstContent.TryGetProperty("text", out var text))
                {
                    return text.GetString() ?? string.Empty;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract text content from MCP response");
        }

        return string.Empty;
    }

    private int GetNextRequestId() => Interlocked.Increment(ref _requestId);

    public void Dispose()
    {
        if (_disposed) return;

        StopMcpServer();
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
