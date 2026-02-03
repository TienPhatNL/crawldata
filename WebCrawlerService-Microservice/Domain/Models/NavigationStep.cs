namespace WebCrawlerService.Domain.Models
{
    /// <summary>
    /// Represents a single step in a navigation strategy
    /// Stored as JSON in NavigationStrategy.NavigationStepsJson
    /// </summary>
    public class NavigationStep
    {
        public int Order { get; set; }
        public string Action { get; set; } = null!;  // "tap", "scroll", "input", "wait", "extract", "navigate", "loop"
        public string? Target { get; set; }  // Element description or selector
        public Dictionary<string, object>? Parameters { get; set; }
        public string? Description { get; set; }
        public bool IsOptional { get; set; }
        public int MaxRetries { get; set; } = 3;
    }
}
