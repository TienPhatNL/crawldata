using WebCrawlerService.Domain.Common;

namespace WebCrawlerService.Domain.Entities;

/// <summary>
/// Tracks group members' access and participation in collaborative crawl jobs
/// </summary>
public class CrawlJobParticipant : BaseEntity
{
    /// <summary>
    /// Crawl job being participated in
    /// </summary>
    public Guid CrawlJobId { get; set; }

    /// <summary>
    /// User participating in the crawl
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Group the participant belongs to (if group crawl)
    /// </summary>
    public Guid? GroupId { get; set; }

    /// <summary>
    /// Participant's role in the crawl
    /// </summary>
    public ParticipantRole Role { get; set; } = ParticipantRole.Viewer;

    /// <summary>
    /// When the participant joined/was added
    /// </summary>
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last time the participant viewed this crawl job
    /// </summary>
    public DateTime? LastViewedAt { get; set; }

    /// <summary>
    /// Whether the participant has been notified of new results
    /// </summary>
    public bool IsNotified { get; set; } = false;

    /// <summary>
    /// Whether the participant is actively watching for updates
    /// </summary>
    public bool IsWatching { get; set; } = true;

    // Navigation properties
    public virtual CrawlJob CrawlJob { get; set; } = null!;
}

/// <summary>
/// Participant's role in a collaborative crawl
/// </summary>
public enum ParticipantRole
{
    /// <summary>
    /// Created the crawl job
    /// </summary>
    Owner = 1,

    /// <summary>
    /// Can view and contribute to the crawl discussion
    /// </summary>
    Collaborator = 2,

    /// <summary>
    /// Can only view the crawl results
    /// </summary>
    Viewer = 3
}
