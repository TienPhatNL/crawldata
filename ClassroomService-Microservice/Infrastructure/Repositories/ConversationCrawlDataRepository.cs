using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ClassroomService.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for ConversationCrawlData
/// </summary>
public class ConversationCrawlDataRepository : Repository<ConversationCrawlData>, IConversationCrawlDataRepository
{
    public ConversationCrawlDataRepository(ClassroomDbContext context) : base(context)
    {
    }

    public async Task<List<ConversationCrawlData>> GetByConversationIdAsync(
        Guid conversationId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.ConversationCrawlData
            .Where(c => c.ConversationId == conversationId && !c.IsDeleted)
            .OrderByDescending(c => c.CrawledAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ConversationCrawlData?> GetByCrawlJobIdAsync(
        Guid crawlJobId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.ConversationCrawlData
            .FirstOrDefaultAsync(
                c => c.CrawlJobId == crawlJobId && !c.IsDeleted, 
                cancellationToken);
    }

    public async Task<List<ConversationCrawlData>> VectorSearchAsync(
        Guid conversationId,
        float[] queryEmbedding,
        int topK = 5,
        double minQuality = 0.0,
        CancellationToken cancellationToken = default)
    {
        // Get all crawl data for the conversation with minimum quality
        var crawlDataList = await _context.ConversationCrawlData
            .Where(c => c.ConversationId == conversationId 
                     && !c.IsDeleted 
                     && c.DataQualityScore >= minQuality
                     && c.VectorEmbeddingJson != null)
            .ToListAsync(cancellationToken);

        if (!crawlDataList.Any())
            return new List<ConversationCrawlData>();

        // Calculate cosine similarity for each item
        var results = new List<(ConversationCrawlData data, double similarity)>();

        foreach (var crawlData in crawlDataList)
        {
            try
            {
                var embedding = JsonSerializer.Deserialize<float[]>(crawlData.VectorEmbeddingJson!);
                if (embedding != null && embedding.Length == queryEmbedding.Length)
                {
                    var similarity = CalculateCosineSimilarity(queryEmbedding, embedding);
                    results.Add((crawlData, similarity));
                }
            }
            catch (JsonException)
            {
                // Skip invalid embeddings
                continue;
            }
        }

        // Return top K results sorted by similarity (descending)
        return results
            .OrderByDescending(r => r.similarity)
            .Take(topK)
            .Select(r => r.data)
            .ToList();
    }

    /// <summary>
    /// Calculate cosine similarity between two vectors
    /// </summary>
    private double CalculateCosineSimilarity(float[] vector1, float[] vector2)
    {
        if (vector1.Length != vector2.Length)
            throw new ArgumentException("Vectors must have the same length");

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
