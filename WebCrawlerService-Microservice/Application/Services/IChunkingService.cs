namespace WebCrawlerService.Application.Services;

/// <summary>
/// Service interface for chunking HTML and text content
/// </summary>
public interface IChunkingService
{
    /// <summary>
    /// Chunk HTML content intelligently by element boundaries
    /// Prevents splitting products/articles mid-element
    /// </summary>
    /// <param name="html">HTML content to chunk</param>
    /// <param name="chunkSize">Size of each chunk in characters (default: 15000)</param>
    /// <param name="overlap">Number of characters to overlap between chunks (default: 500)</param>
    /// <returns>List of HTML chunks</returns>
    List<string> ChunkHtml(string html, int chunkSize = 15000, int overlap = 500);

    /// <summary>
    /// Chunk text content (non-HTML) into overlapping segments
    /// Tries to break at sentence boundaries
    /// </summary>
    /// <param name="text">Text content to chunk</param>
    /// <param name="chunkSize">Size of each chunk in characters (default: 2000)</param>
    /// <param name="overlap">Number of characters to overlap between chunks (default: 200)</param>
    /// <returns>List of text chunks</returns>
    List<string> ChunkText(string text, int chunkSize = 2000, int overlap = 200);
}
