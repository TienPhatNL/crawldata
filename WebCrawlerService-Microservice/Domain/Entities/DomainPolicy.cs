using WebCrawlerService.Domain.Enums;
using WebCrawlerService.Domain.Common;

namespace WebCrawlerService.Domain.Entities
{
    public class DomainPolicy : BaseAuditableEntity
    {
        public string DomainPattern { get; set; } = null!; // Regex pattern
        public DomainPolicyType PolicyType { get; set; } // Allow, Block, Restricted
        public string? Reason { get; set; }
        public bool IsActive { get; set; } = true;
        
        // Rate limiting for domain
        public int? MaxRequestsPerMinute { get; set; }
        public int? MaxConcurrentRequests { get; set; }
        public int? DelayBetweenRequestsMs { get; set; }
        
        // Restrictions
        public SubscriptionTier? MinimumTierRequired { get; set; }
        public UserRole[] AllowedRoles { get; set; } = Array.Empty<UserRole>();
        
        public DateTime? LastUpdated { get; set; }
    }
}