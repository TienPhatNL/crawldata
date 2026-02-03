using WebCrawlerService.Domain.Common;
using WebCrawlerService.Domain.Enums;

namespace WebCrawlerService.Domain.Entities
{
    public class CrawlJob : BaseAuditableEntity, ISoftDelete
    {
        public Guid UserId { get; set; }
        public Guid? AssignmentId { get; set; } // For educational context
        public string[] Urls { get; set; } = Array.Empty<string>();
        public JobStatus Status { get; set; } = JobStatus.Pending;
        public Priority Priority { get; set; } = Priority.Normal;
        public CrawlerType CrawlerType { get; set; } = CrawlerType.HttpClient;

        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? FailedAt { get; set; }

        public string? ErrorMessage { get; set; }
        public int RetryCount { get; set; } = 0;
        public int MaxRetries { get; set; } = 3;
        public DateTime? NextRetryAt { get; set; }

        // Configuration
        public string ConfigurationJson { get; set; } = "{}";
        public int TimeoutSeconds { get; set; } = 30;
        public bool FollowRedirects { get; set; } = true;
        public bool ExtractImages { get; set; } = false;
        public bool ExtractLinks { get; set; } = true;

        // Intelligent Crawl Fields (NEW)
        /// <summary>
        /// User's natural language prompt for data extraction
        /// Example: "Extract product name, price, and reviews"
        /// </summary>
        public string? UserPrompt { get; set; }

        /// <summary>
        /// Reference to template if template-based crawling is used
        /// </summary>
        public Guid? TemplateId { get; set; }

        /// <summary>
        /// AI-generated extraction strategy (JSON serialized)
        /// Contains selectors, field definitions, and extraction logic
        /// </summary>
        public string? ExtractionStrategyJson { get; set; }

        /// <summary>
        /// User's crawler preference (Auto, PlaywrightAI, Template, etc.)
        /// </summary>
        public CrawlerPreference CrawlerPreference { get; set; } = CrawlerPreference.Auto;

        /// <summary>
        /// Whether to automatically solve CAPTCHAs (requires Pro tier)
        /// </summary>
        public bool AutoSolveCaptcha { get; set; } = false;

        /// <summary>
        /// Whether to use proxy rotation for this job
        /// </summary>
        public bool UseProxyRotation { get; set; } = false;

        // Results summary
        public int UrlsProcessed { get; set; } = 0;
        public int UrlsSuccessful { get; set; } = 0;
        public int UrlsFailed { get; set; } = 0;
        public long TotalContentSize { get; set; } = 0;
        public int ResultCount { get; set; } = 0;

        // Conversation context
        public Guid? ConversationId { get; set; }  // Link related prompts
        public Guid? ParentPromptId { get; set; }  // Source prompt
        
        /// <summary>
        /// AI-generated conversation name for this crawl (from Python agent)
        /// </summary>
        public string? ConversationName { get; set; }

        // Group collaboration support (NEW for chat-based crawler)
        /// <summary>
        /// Group ID from ClassroomService for collaborative crawls
        /// </summary>
        public Guid? GroupId { get; set; }

        /// <summary>
        /// Conversation thread ID from ClassroomService crawler chat
        /// </summary>
        public Guid? ConversationThreadId { get; set; }

        /// <summary>
        /// Access level for this crawl job
        /// </summary>
        public CrawlAccessLevel AccessLevel { get; set; } = CrawlAccessLevel.Private;

        /// <summary>
        /// Whether this is a collaborative crawl shared with group members
        /// </summary>
        public bool IsCollaborative { get; set; } = false;

        // Navigation strategy
        public Guid? NavigationStrategyId { get; set; }
        public string? NavigationProgressJson { get; set; }
        public int CurrentNavigationStep { get; set; } = 0;
        public NavigationSessionType SessionType { get; set; } = NavigationSessionType.Continuous;

        // Agent assignment (new pool-based)
        public Guid? AssignedAgentPoolId { get; set; }

        // Soft delete support
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public Guid? DeletedBy { get; set; }

        // Navigation properties
        public virtual ICollection<CrawlResult> Results { get; set; } = new List<CrawlResult>();
        public virtual CrawlerAgent? AssignedAgent { get; set; }
        public Guid? AssignedAgentId { get; set; }
        public virtual CrawlTemplate? Template { get; set; }
        public virtual AgentPool? AssignedAgentPool { get; set; }
        public virtual NavigationStrategy? NavigationStrategy { get; set; }
        public virtual PromptHistory? ParentPrompt { get; set; }
        public virtual ICollection<CrawlJobParticipant> Participants { get; set; } = new List<CrawlJobParticipant>();
    }

    /// <summary>
    /// Access level for crawl jobs
    /// </summary>
    public enum CrawlAccessLevel
    {
        /// <summary>
        /// Only the creator can view
        /// </summary>
        Private = 1,

        /// <summary>
        /// All group members can view
        /// </summary>
        Group = 2,

        /// <summary>
        /// All students in assignment can view (public crawl)
        /// </summary>
        Assignment = 3
    }
}