namespace NotificationService.Domain.Enums;

public enum NotificationType
{
    System,
    CourseUpdate,
    AssignmentCreated,
    AssignmentDueReminder,
    AssignmentGraded,
    AssignmentClosed,
    EnrollmentApproved,
    EnrollmentRejected,
    GroupInvitation,
    GroupMemberAdded,
    GroupMemberRemoved,
    CrawlJobCompleted,
    CrawlJobFailed,
    UserMention,
    ReportGenerated,
    SubscriptionExpiring,
    CourseApproved,
    CourseRejected,
    // Report specific types
    ReportSubmitted,
    ReportGraded,
    RevisionRequested,
    ReportReverted,
    // Support request types
    SupportRequestRejected,
    // WebCrawler specific types
    CrawlJobStarted,
    CrawlJobProgress,
    CrawlJobStatusChanged,
    UrlCrawlFailed
}
