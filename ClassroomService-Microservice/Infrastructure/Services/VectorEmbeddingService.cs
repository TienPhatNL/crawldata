using ClassroomService.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace ClassroomService.Infrastructure.Services;

/// <summary>
/// Service for generating vector embeddings using Python Crawl4AI agent (Gemini API)
/// </summary>
public class VectorEmbeddingService : IVectorEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<VectorEmbeddingService> _logger;
    private readonly string _pythonAgentUrl;

    public VectorEmbeddingService(
        HttpClient httpClient,
        ILogger<VectorEmbeddingService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _pythonAgentUrl = configuration["Services:PythonAgent:BaseUrl"] 
            ?? "http://localhost:8000";
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            // Truncate text if too long
            if (text.Length > 10000)
            {
                text = text.Substring(0, 10000);
                _logger.LogWarning("Text truncated to 10000 characters for embedding");
            }

            var requestBody = new
            {
                text,
                model = "models/embedding-001"
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_pythonAgentUrl}/embedding",
                requestBody,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Python agent returned error: {StatusCode}", response.StatusCode);
                return new float[768]; // Return zero vector as fallback
            }

            var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
            
            if (!result.TryGetProperty("embedding", out var embeddingElement))
            {
                _logger.LogError("No embedding in response");
                return new float[768];
            }

            var embedding = embeddingElement
                .EnumerateArray()
                .Select(e => (float)e.GetDouble())
                .ToArray();

            _logger.LogInformation("Generated embedding with {Dimensions} dimensions", embedding.Length);

            return embedding;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate embedding");
            // Return zero vector as fallback
            return new float[768]; // Gemini embedding-001 uses 768 dimensions
        }
    }

    public double CosineSimilarity(float[] vector1, float[] vector2)
    {
        if (vector1.Length != vector2.Length)
        {
            _logger.LogWarning("Vector length mismatch: {Len1} vs {Len2}", vector1.Length, vector2.Length);
            return 0;
        }

        double dotProduct = 0;
        double magnitude1 = 0;
        double magnitude2 = 0;

        for (int i = 0; i < vector1.Length; i++)
        {
            dotProduct += vector1[i] * vector2[i];
            magnitude1 += vector1[i] * vector1[i];
            magnitude2 += vector2[i] * vector2[i];
        }

        magnitude1 = Math.Sqrt(magnitude1);
        magnitude2 = Math.Sqrt(magnitude2);

        if (magnitude1 == 0 || magnitude2 == 0)
            return 0;

        return dotProduct / (magnitude1 * magnitude2);
    }
}
