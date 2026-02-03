namespace WebCrawlerService.Domain.Models
{
    /// <summary>
    /// Result of executing a single navigation step
    /// </summary>
    public class StepResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string? Message { get; set; }
        public List<object>? Data { get; set; }
    }
}
