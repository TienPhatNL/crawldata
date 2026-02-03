namespace WebCrawlerService.Domain.Models
{
    /// <summary>
    /// Result of executing a navigation strategy
    /// </summary>
    public class NavigationResult
    {
        public bool Success { get; set; }
        public double ExecutionTimeMs { get; set; }
        public int CompletedSteps { get; set; }
        public int TotalSteps { get; set; }
        public int? FailedStep { get; set; }
        public string? Error { get; set; }
        public List<object>? ExtractedData { get; set; }
        public string? PageSnapshot { get; set; }  // For debugging/refinement
    }
}
