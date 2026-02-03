using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace ClassroomService.Infrastructure.Services;

/// <summary>
/// Service for normalizing crawl data and storing it in conversation context
/// </summary>
public class CrawlDataNormalizationService : ICrawlDataNormalizationService
{
    private readonly ICrawlerIntegrationService _crawlerService;
    private readonly IVectorEmbeddingService _embeddingService;
    private readonly IDataValidator _dataValidator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CrawlDataNormalizationService> _logger;

    public CrawlDataNormalizationService(
        ICrawlerIntegrationService crawlerService,
        IVectorEmbeddingService embeddingService,
        IDataValidator dataValidator,
        IUnitOfWork unitOfWork,
        ILogger<CrawlDataNormalizationService> logger)
    {
        _crawlerService = crawlerService;
        _embeddingService = embeddingService;
        _dataValidator = dataValidator;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ConversationCrawlData> NormalizeAndStoreAsync(
        Guid conversationId,
        Guid crawlJobId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting normalization for conversation {ConversationId}, job {CrawlJobId}",
                conversationId, crawlJobId);

            // 1. Fetch raw crawl results from WebCrawlerService
            var rawResults = await _crawlerService.GetCrawlResultsAsync(crawlJobId, cancellationToken);

            if (rawResults == null || !rawResults.Any())
            {
                _logger.LogWarning("No crawl results found for job {CrawlJobId}", crawlJobId);
                throw new InvalidOperationException($"No crawl results found for job {crawlJobId}");
            }

            // 2. Extract and combine all ExtractedDataJson
            var combinedData = CombineExtractedData(rawResults);

            // 3. Normalize and validate (handle broken data)
            var validated = await _dataValidator.ValidateAndCleanAsync(combinedData);

            _logger.LogInformation("Validation complete: {ValidCount} valid, {InvalidCount} invalid records",
                validated.ValidRecords.Count, validated.InvalidRecords.Count);

            // 4. Detect schema automatically
            var schema = _dataValidator.DetectSchema(validated.ValidRecords);

            _logger.LogInformation("Detected schema type: {SchemaType}, fields: {FieldCount}",
                schema.Type, schema.Fields.Count);

            // 5. Generate embedding text
            var embeddingText = GenerateEmbeddingText(
                rawResults.FirstOrDefault()?.Url ?? "Unknown",
                validated.ValidRecords,
                schema);

            // 6. Generate vector embedding
            float[] embedding;
            try
            {
                embedding = await _embeddingService.GenerateEmbeddingAsync(embeddingText, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate embedding, using zero vector");
                embedding = new float[768]; // Fallback to zero vector
            }

            // 7. Prepare normalized data JSON
            var normalizedDataJson = JsonSerializer.Serialize(new
            {
                data = validated.ValidRecords,
                validation_warnings = validated.Warnings
            });

            var validationWarningsJson = validated.Warnings.Any()
                ? JsonSerializer.Serialize(validated.Warnings)
                : null;

            var detectedSchemaJson = JsonSerializer.Serialize(schema);
            var vectorEmbeddingJson = JsonSerializer.Serialize(embedding);

            // 8. Create and save ConversationCrawlData
            var conversationData = new ConversationCrawlData
            {
                ConversationId = conversationId,
                CrawlJobId = crawlJobId,
                SourceUrl = rawResults.FirstOrDefault()?.Url ?? "",
                CrawledAt = DateTime.UtcNow,
                ResultCount = rawResults.Count,
                NormalizedDataJson = normalizedDataJson,
                ValidRecordCount = validated.ValidRecords.Count,
                InvalidRecordCount = validated.InvalidRecords.Count,
                DataQualityScore = validated.QualityScore,
                ValidationWarningsJson = validationWarningsJson,
                DetectedSchemaJson = detectedSchemaJson,
                EmbeddingText = embeddingText,
                VectorEmbeddingJson = vectorEmbeddingJson
            };

            await _unitOfWork.ConversationCrawlData.AddAsync(conversationData, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully normalized and stored crawl data: ID {DataId}, Quality {Quality:P0}",
                conversationData.Id, conversationData.DataQualityScore);

            return conversationData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to normalize crawl data for job {CrawlJobId}", crawlJobId);
            throw;
        }
    }

    private List<object> CombineExtractedData(List<CrawlResultDetailDto> rawResults)
    {
        var combined = new List<object>();

        foreach (var result in rawResults)
        {
            if (string.IsNullOrWhiteSpace(result.ExtractedDataJson))
                continue;

            try
            {
                var data = JsonSerializer.Deserialize<JsonElement>(result.ExtractedDataJson);

                // Handle both array and single object
                if (data.ValueKind == JsonValueKind.Array)
                {
                    foreach (var element in data.EnumerateArray())
                    {
                        combined.Add(element);
                    }
                }
                else
                {
                    combined.Add(data);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse ExtractedDataJson from result {ResultId}", result.ResultId);
            }
        }

        return combined;
    }

    private string GenerateEmbeddingText(
        string sourceUrl,
        List<object> validRecords,
        DataSchema schema)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Source: {sourceUrl}");
        sb.AppendLine($"Data Type: {schema.Type}");
        sb.AppendLine($"Record Count: {validRecords.Count}");
        sb.AppendLine($"Fields: {string.Join(", ", schema.Fields)}");

        // Include sample data (first 3 records for context - reduced for efficiency)
        var sampleCount = Math.Min(3, validRecords.Count);
        if (sampleCount > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"Sample Data ({sampleCount} records):");

            for (int i = 0; i < sampleCount; i++)
            {
                var record = validRecords[i];
                var recordJson = JsonSerializer.Serialize(record);
                
                // Truncate long records
                if (recordJson.Length > 150)
                    recordJson = recordJson.Substring(0, 150) + "...";
                
                sb.AppendLine($"{i + 1}. {recordJson}");
            }
        }

        // Add aggregated statistics if available
        if (schema.ValueField != null && validRecords.Count > 0)
        {
            try
            {
                var values = ExtractNumericValues(validRecords, schema.ValueField);
                if (values.Any())
                {
                    sb.AppendLine();
                    sb.AppendLine($"Statistics for {schema.ValueField}:");
                    sb.AppendLine($"- Count: {values.Count}");
                    sb.AppendLine($"- Min: {values.Min():F2}");
                    sb.AppendLine($"- Max: {values.Max():F2}");
                    sb.AppendLine($"- Average: {values.Average():F2}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to calculate statistics");
            }
        }

        return sb.ToString();
    }

    private List<double> ExtractNumericValues(List<object> records, string fieldName)
    {
        var values = new List<double>();

        foreach (var record in records)
        {
            try
            {
                var dict = record as Dictionary<string, object?>
                    ?? JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        JsonSerializer.Serialize(record));

                if (dict != null && dict.TryGetValue(fieldName, out var value))
                {
                    var numValue = Convert.ToDouble(value);
                    if (!double.IsNaN(numValue) && !double.IsInfinity(numValue))
                        values.Add(numValue);
                }
            }
            catch
            {
                // Skip non-numeric values
            }
        }

        return values;
    }
}
