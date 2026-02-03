using WebCrawlerService.Domain.Common;

namespace WebCrawlerService.Domain.Entities
{
    public class CrawlResult : BaseAuditableEntity
    {
        public Guid CrawlJobId { get; set; }
        public string Url { get; set; } = null!;
        public int HttpStatusCode { get; set; }
        public string? ContentType { get; set; }
        public long ContentSize { get; set; }
        public string? Content { get; set; } // Stored content
        public string? ContentHash { get; set; } // For deduplication

        public DateTime CrawledAt { get; set; }
        public int ResponseTimeMs { get; set; }

        // Extracted metadata
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Keywords { get; set; }
        public string[] Images { get; set; } = Array.Empty<string>();
        public string[] Links { get; set; } = Array.Empty<string>();

        // Intelligent Extraction Fields (NEW)
        /// <summary>
        /// Structured extracted data as JSON (AI or template-based)
        /// Example: {"productName": "iPhone 15", "price": 25000000, "rating": 4.5}
        /// </summary>
        public string? ExtractedDataJson { get; set; }

        /// <summary>
        /// The prompt used for extraction (if intelligent mode was used)
        /// </summary>
        public string? PromptUsed { get; set; }

        /// <summary>
        /// Template ID if template-based extraction was used
        /// </summary>
        public Guid? TemplateId { get; set; }

        /// <summary>
        /// Template version used for extraction
        /// </summary>
        public string? TemplateVersion { get; set; }

        /// <summary>
        /// Confidence score of extraction (0.0 to 1.0)
        /// Higher score means more reliable extraction
        /// </summary>
        public double ExtractionConfidence { get; set; } = 0.0;

        /// <summary>
        /// Cost of LLM API calls for this result (in USD)
        /// </summary>
        public decimal LlmCost { get; set; } = 0.0m;

        /// <summary>
        /// Number of CAPTCHAs solved during this crawl
        /// </summary>
        public int CaptchasSolved { get; set; } = 0;

        /// <summary>
        /// Cost of CAPTCHA solving for this result (in USD)
        /// </summary>
        public decimal CaptchaCost { get; set; } = 0.0m;

        /// <summary>
        /// Whether a proxy was used for this crawl
        /// </summary>
        public bool ProxyUsed { get; set; } = false;

        /// <summary>
        /// Screenshot of the page as base64 string (for validation/debugging)
        /// </summary>
        public string? ScreenshotBase64 { get; set; }

        /// <summary>
        /// Any extraction warnings or issues
        /// </summary>
        public string[]? ExtractionWarnings { get; set; }

        /// <summary>
        /// Error message if the crawl failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        // Analysis flags
        public bool IsSuccess { get; set; } = false;
        public bool IsAnalyzed { get; set; } = false;
        public DateTime? AnalyzedAt { get; set; }
        public string? AnalysisResultId { get; set; }

        // Convenience property for consistency with legacy code
        public int StatusCode => HttpStatusCode;

        // Navigation properties
        public virtual CrawlJob? CrawlJob { get; set; } // Nullable to support soft-deleted parent jobs
    }
}