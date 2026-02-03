using System.Net;
using System.Text.Json.Serialization;

namespace CrawlData.ApiGateway.Models;

public class ResponseModel
{
    [JsonPropertyName("status")]
    public HttpStatusCode Status { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public object? Data { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; set; }

    public ResponseModel()
    {
    }

    public ResponseModel(HttpStatusCode status, string message)
    {
        Status = status;
        Message = message;
    }

    public ResponseModel(HttpStatusCode status, string message, object? data)
    {
        Status = status;
        Message = message;
        Data = data;
    }

    public ResponseModel(HttpStatusCode status, string message, object? data, string? correlationId)
    {
        Status = status;
        Message = message;
        Data = data;
        CorrelationId = correlationId;
    }

    // Static factory methods for common responses
    public static ResponseModel Success(string message = "Success") => 
        new(HttpStatusCode.OK, message);

    public static ResponseModel Success(object data, string message = "Success") => 
        new(HttpStatusCode.OK, message, data);

    public static ResponseModel Error(string message) => 
        new(HttpStatusCode.InternalServerError, message);

    public static ResponseModel BadRequest(string message) => 
        new(HttpStatusCode.BadRequest, message);

    public static ResponseModel Unauthorized(string message = "Unauthorized") => 
        new(HttpStatusCode.Unauthorized, message);

    public static ResponseModel Forbidden(string message = "Forbidden") => 
        new(HttpStatusCode.Forbidden, message);

    public static ResponseModel NotFound(string message = "Not Found") => 
        new(HttpStatusCode.NotFound, message);

    public static ResponseModel TooManyRequests(string message = "Too Many Requests") => 
        new(HttpStatusCode.TooManyRequests, message);
}