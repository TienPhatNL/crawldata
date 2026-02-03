using Microsoft.EntityFrameworkCore;
using ClassroomService.Domain.Common;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Infrastructure.Persistence;

public class ClassroomDbContext : DbContext
{
    public ClassroomDbContext(DbContextOptions<ClassroomDbContext> options) : base(options)
    {
    }

    public DbSet<Term> Terms { get; set; }
    public DbSet<CourseCode> CourseCodes { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<CourseRequest> CourseRequests { get; set; }
    public DbSet<CourseEnrollment> CourseEnrollments { get; set; }
    public DbSet<Assignment> Assignments { get; set; }
    public DbSet<Topic> Topics { get; set; }
    public DbSet<TopicWeight> TopicWeights { get; set; }
    public DbSet<TopicWeightHistory> TopicWeightHistories { get; set; }
    public DbSet<Group> Groups { get; set; }
    public DbSet<GroupMember> GroupMembers { get; set; }
    public DbSet<Chat> Chats { get; set; }
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<SupportRequest> SupportRequests { get; set; }
    public DbSet<Report> Reports { get; set; }
    public DbSet<ReportHistory> ReportHistories { get; set; }
    public DbSet<CrawlerChatMessage> CrawlerChatMessages { get; set; }
    public DbSet<ReportCrawlData> ReportCrawlData { get; set; }
    public DbSet<ReportAICheck> ReportAIChecks { get; set; }
    public DbSet<ConversationCrawlData> ConversationCrawlData { get; set; }
    public DbSet<ConversationUploadedFile> ConversationUploadedFiles { get; set; }
    public DbSet<TemplateFile> TemplateFiles { get; set; }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<BaseEntity>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }

            // Handle auditable entities
            if (entry.Entity is BaseAuditableEntity auditableEntity)
            {
                if (entry.State == EntityState.Modified)
                {
                    auditableEntity.LastModifiedAt = DateTime.UtcNow;
                }
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Ignore BaseEvent and all domain event classes (they are not database entities)
        modelBuilder.Ignore<BaseEvent>();
        
        // Get all types that inherit from BaseEvent and ignore them
        var eventTypes = typeof(BaseEvent).Assembly.GetTypes()
            .Where(t => typeof(BaseEvent).IsAssignableFrom(t) && !t.IsAbstract);
        
        foreach (var eventType in eventTypes)
        {
            modelBuilder.Ignore(eventType);
        }

        // Term configuration
        modelBuilder.Entity<Term>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.StartDate).IsRequired();
            entity.Property(e => e.EndDate).IsRequired();
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);

            // Indexes
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => new { e.StartDate, e.EndDate });
        });

        // CourseCode configuration
        modelBuilder.Entity<CourseCode>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Code).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Department).HasMaxLength(100).IsRequired();
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);

            // Indexes
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.Department);
            entity.HasIndex(e => e.IsActive);
        });

        // Course configuration (updated)
        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Foreign key to CourseCode
            entity.Property(e => e.CourseCodeId).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(300).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.TermId).IsRequired();
            
            // Course Status
            entity.Property(e => e.Status)
                .IsRequired()
                .HasDefaultValue(CourseStatus.PendingApproval)
                .HasConversion<int>();
            
            entity.Property(e => e.ApprovalComments).HasMaxLength(500);
            entity.Property(e => e.RejectionReason).HasMaxLength(500);
            
            entity.Property(e => e.LecturerId).IsRequired();
            
            // Optional course image
            entity.Property(e => e.Img).HasMaxLength(1000);
            
            // Access Code properties
            entity.Property(e => e.AccessCode).HasMaxLength(50);
            entity.Property(e => e.RequiresAccessCode).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.AccessCodeAttempts).IsRequired().HasDefaultValue(0);

            // Relationships
            entity.HasOne(e => e.CourseCode)
                .WithMany(cc => cc.Courses)
                .HasForeignKey(e => e.CourseCodeId)
                .OnDelete(DeleteBehavior.Restrict); // Don't allow deletion of CourseCode if courses exist

            entity.HasOne(e => e.Term)
                .WithMany(t => t.Courses)
                .HasForeignKey(e => e.TermId)
                .OnDelete(DeleteBehavior.Restrict); // Don't allow deletion of Term if courses exist

            // Indexes
            entity.HasIndex(e => e.CourseCodeId);
            entity.HasIndex(e => e.TermId);
            entity.HasIndex(e => e.LecturerId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ApprovedBy);
            entity.HasIndex(e => e.UniqueCode).IsUnique(); // Unique code for course identification
            entity.HasIndex(e => new { e.CourseCodeId, e.TermId, e.LecturerId, e.UniqueCode }).IsUnique(); // Unique course section per lecturer with unique code
            entity.HasIndex(e => e.AccessCode).HasFilter("AccessCode IS NOT NULL AND RequiresAccessCode = 1");
            entity.HasIndex(e => e.RequiresAccessCode).HasFilter("RequiresAccessCode = 1");
        });

        // CourseRequest configuration
        modelBuilder.Entity<CourseRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Foreign key to CourseCode
            entity.Property(e => e.CourseCodeId).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.TermId).IsRequired();
            
            // Note: LecturerId and ProcessedBy reference User in UserService - no FK constraint
            entity.Property(e => e.LecturerId).IsRequired();
            entity.Property(e => e.ProcessedBy).IsRequired(false);
            
            entity.Property(e => e.Status)
                .IsRequired()
                .HasDefaultValue(CourseRequestStatus.Pending)
                .HasConversion<int>()
                .HasSentinel(CourseRequestStatus.Cancelled);
            entity.Property(e => e.RequestReason).HasMaxLength(500);
            entity.Property(e => e.ProcessingComments).HasMaxLength(500);

            // Relationships
            entity.HasOne(e => e.CourseCode)
                .WithMany() // CourseCode doesn't have a navigation property back to CourseRequests
                .HasForeignKey(e => e.CourseCodeId)
                .OnDelete(DeleteBehavior.Restrict); // Don't allow deletion of CourseCode if requests exist

            entity.HasOne(e => e.Term)
                .WithMany() // Term doesn't have a navigation property back to CourseRequests
                .HasForeignKey(e => e.TermId)
                .OnDelete(DeleteBehavior.Restrict); // Don't allow deletion of Term if requests exist

            entity.HasOne(e => e.CreatedCourse)
                .WithMany() // Course doesn't have a navigation property back to CourseRequests
                .HasForeignKey(e => e.CreatedCourseId)
                .OnDelete(DeleteBehavior.SetNull) // If course is deleted, set the reference to null
                .IsRequired(false);

            // Indexes
            entity.HasIndex(e => e.CourseCodeId);
            entity.HasIndex(e => e.TermId);
            entity.HasIndex(e => e.LecturerId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ProcessedBy);
            entity.HasIndex(e => new { e.CourseCodeId, e.TermId, e.LecturerId, e.Status })
                .HasFilter("Status = 1"); // Index for pending requests
        });

        // CourseEnrollment configuration
        modelBuilder.Entity<CourseEnrollment>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Relationships
            entity.HasOne(e => e.Course)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Note: StudentId references User in UserService - no FK constraint
            entity.Property(e => e.StudentId).IsRequired();
            
            // Status and tracking properties with sentinel value to fix EF Core warning
            entity.Property(e => e.Status)
                .IsRequired()
                .HasDefaultValue(EnrollmentStatus.Active)
                .HasSentinel(EnrollmentStatus.Inactive); // Use Inactive as sentinel to avoid conflict with Active default
            entity.Property(e => e.UnenrollmentReason).HasMaxLength(500);

            // Unique constraint - only one active enrollment per student per course
            entity.HasIndex(e => new { e.CourseId, e.StudentId }).IsUnique();
            
            // Additional indexes for querying
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.JoinedAt);
        });

        // Topic configuration
        modelBuilder.Entity<Topic>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);

            // Indexes
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.IsActive);
        });

        // TopicWeight configuration
        modelBuilder.Entity<TopicWeight>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.WeightPercentage).IsRequired().HasPrecision(5, 2);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.ConfiguredBy).IsRequired();
            entity.Property(e => e.IsDeleted).IsRequired().HasDefaultValue(false);

            // Relationships
            entity.HasOne(e => e.Topic)
                .WithMany(t => t.TopicWeights)
                .HasForeignKey(e => e.TopicId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CourseCode)
                .WithMany(cc => cc.TopicWeights)
                .HasForeignKey(e => e.CourseCodeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.SpecificCourse)
                .WithMany(c => c.CustomTopicWeights)
                .HasForeignKey(e => e.SpecificCourseId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            entity.HasIndex(e => new { e.TopicId, e.CourseCodeId });
            entity.HasIndex(e => new { e.TopicId, e.SpecificCourseId });
            entity.HasIndex(e => e.ConfiguredBy);
            entity.HasIndex(e => e.IsDeleted); // For soft delete queries
            
            // Global query filter: Exclude soft-deleted records by default
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // TopicWeightHistory configuration
        modelBuilder.Entity<TopicWeightHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.TermName).HasMaxLength(100);
            entity.Property(e => e.OldWeightPercentage).HasPrecision(5, 2);
            entity.Property(e => e.NewWeightPercentage).IsRequired().HasPrecision(5, 2);
            entity.Property(e => e.ModifiedBy).IsRequired();
            entity.Property(e => e.ModifiedAt).IsRequired();
            entity.Property(e => e.ChangeReason).HasMaxLength(500);
            entity.Property(e => e.AffectedTerms).HasMaxLength(200);
            
            // Convert enum to string for better readability in database
            entity.Property(e => e.Action)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            // Relationships - ALL set to NoAction to prevent cascade path conflicts
            entity.HasOne(e => e.TopicWeight)
                .WithMany()
                .HasForeignKey(e => e.TopicWeightId)
                .OnDelete(DeleteBehavior.NoAction); // Prevent cascade conflicts

            entity.HasOne(e => e.Topic)
                .WithMany()
                .HasForeignKey(e => e.TopicId)
                .OnDelete(DeleteBehavior.NoAction); // Prevent cascade conflicts

            entity.HasOne(e => e.CourseCode)
                .WithMany()
                .HasForeignKey(e => e.CourseCodeId)
                .OnDelete(DeleteBehavior.NoAction); // Prevent cascade conflicts

            entity.HasOne(e => e.SpecificCourse)
                .WithMany()
                .HasForeignKey(e => e.SpecificCourseId)
                .OnDelete(DeleteBehavior.NoAction); // Prevent cascade conflicts

            entity.HasOne(e => e.Term)
                .WithMany()
                .HasForeignKey(e => e.TermId)
                .OnDelete(DeleteBehavior.NoAction); // Prevent cascade conflicts

            // Indexes for common queries
            entity.HasIndex(e => e.TopicWeightId);
            entity.HasIndex(e => e.TopicId);
            entity.HasIndex(e => e.CourseCodeId);
            entity.HasIndex(e => e.SpecificCourseId);
            entity.HasIndex(e => e.TermId);
            entity.HasIndex(e => e.ModifiedAt);
            entity.HasIndex(e => e.Action);
        });

        // Assignment configuration
        modelBuilder.Entity<Assignment>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(10000);
            entity.Property(e => e.StartDate).IsRequired(false);
            entity.Property(e => e.DueDate).IsRequired();
            entity.Property(e => e.ExtendedDueDate).IsRequired(false);
            entity.Property(e => e.Format).HasMaxLength(100);
            
            // Status configuration - default to Draft with sentinel value
            entity.Property(e => e.Status)
                .IsRequired()
                .HasDefaultValue(AssignmentStatus.Draft)
                .HasConversion<int>()
                .HasSentinel(AssignmentStatus.Closed); // Use Closed as sentinel to avoid conflict with Draft default
            
            entity.Property(e => e.IsGroupAssignment).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MaxPoints).IsRequired(false);
            
            // WeightPercentageSnapshot - Store historical weight for fair grade calculation
            entity.Property(e => e.WeightPercentageSnapshot)
                .HasPrecision(5, 2) // Max 999.99%
                .IsRequired(false); // Nullable for backward compatibility
            
            // Relationships
            entity.HasOne(e => e.Course)
                .WithMany(c => c.Assignments)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Topic)
                .WithMany(t => t.Assignments)
                .HasForeignKey(e => e.TopicId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            entity.HasIndex(e => e.CourseId);
            entity.HasIndex(e => e.TopicId);
            entity.HasIndex(e => e.DueDate);
            entity.HasIndex(e => e.StartDate);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.IsGroupAssignment);
        });

        // Group configuration (enhanced)
        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.MaxMembers).IsRequired(false);
            entity.Property(e => e.IsLocked).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.AssignmentId).IsRequired(false);
            
            // Relationships
            entity.HasOne(e => e.Course)
                .WithMany(c => c.Groups)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // FIXED: Changed from SetNull to Restrict to avoid cascade path conflicts in SQL Server
            entity.HasOne(e => e.Assignment)
                .WithMany(a => a.AssignedGroups)
                .HasForeignKey(e => e.AssignmentId)
                .OnDelete(DeleteBehavior.Restrict) // Must manually handle assignment deletion
                .IsRequired(false);

            // Indexes
            entity.HasIndex(e => e.CourseId);
            entity.HasIndex(e => e.AssignmentId);
            entity.HasIndex(e => e.IsLocked);
            entity.HasIndex(e => new { e.CourseId, e.Name }); // Unique group name per course
        });

        // GroupMember configuration (updated to reference CourseEnrollment)
        modelBuilder.Entity<GroupMember>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.GroupId).IsRequired();
            entity.Property(e => e.EnrollmentId).IsRequired();
            entity.Property(e => e.IsLeader).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.Role)
                .IsRequired()
                .HasDefaultValue(GroupMemberRole.Member);
            entity.Property(e => e.JoinedAt).IsRequired();
            entity.Property(e => e.Notes).HasMaxLength(500);
            
            // Relationships
            entity.HasOne(e => e.Group)
                .WithMany(g => g.Members)
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.Cascade); // Delete members when group is deleted
            
            entity.HasOne(e => e.Enrollment)
                .WithMany() // CourseEnrollment doesn't need back-collection
                .HasForeignKey(e => e.EnrollmentId)
                .OnDelete(DeleteBehavior.NoAction); 
            
            // Unique constraint - student (via enrollment) can only be in a group once
            entity.HasIndex(e => new { e.GroupId, e.EnrollmentId }).IsUnique();
            
            // Additional indexes for querying
            entity.HasIndex(e => e.EnrollmentId);
            entity.HasIndex(e => e.IsLeader);
            entity.HasIndex(e => e.JoinedAt);
            entity.HasIndex(e => e.Role);
            
            // Ignore computed property
            entity.Ignore(e => e.StudentId);
        });

        // Chat configuration
        modelBuilder.Entity<Chat>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Message).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.ConversationId).IsRequired();
            entity.Property(e => e.IsDeleted).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.IsRead).IsRequired().HasDefaultValue(false);
            
            // Relationships
            entity.HasOne(e => e.Course)
                .WithMany(c => c.Chats)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(e => e.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.SupportRequest)
                .WithMany()
                .HasForeignKey(e => e.SupportRequestId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            // Indexes
            entity.HasIndex(e => new { e.ConversationId, e.SentAt });
            entity.HasIndex(e => e.CourseId);
            entity.HasIndex(e => e.SenderId);
            entity.HasIndex(e => e.ReceiverId);
            entity.HasIndex(e => e.IsDeleted);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.SupportRequestId); // Index for filtering messages by support request
            entity.HasIndex(e => new { e.ReceiverId, e.IsRead }); // Index for unread message queries
        });

        // Conversation configuration
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.LastMessagePreview).HasMaxLength(100);
            entity.Property(e => e.LastMessageAt).IsRequired();
            entity.Property(e => e.IsCrawler).IsRequired().HasDefaultValue(false);
            
            // Unique constraint: ONE non-crawler conversation per user pair per course
            entity.HasIndex(e => new { e.CourseId, e.User1Id, e.User2Id })
                .IsUnique()
                .HasFilter("[IsCrawler] = 0");
            
            // Indexes for querying user's conversations
            entity.HasIndex(e => new { e.User1Id, e.LastMessageAt });
            entity.HasIndex(e => new { e.User2Id, e.LastMessageAt });
            
            // Relationships
            entity.HasOne(e => e.Course)
                .WithMany()
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Report configuration
        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Submission).HasMaxLength(50000).IsRequired();
            entity.Property(e => e.Feedback).HasMaxLength(5000);
            entity.Property(e => e.Grade).HasColumnType("decimal(5,2)");
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.Version).IsRequired().HasDefaultValue(1);
            
            // Relationships
            entity.HasOne(e => e.Assignment)
                .WithMany(a => a.Reports)
                .HasForeignKey(e => e.AssignmentId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.Group)
                .WithMany(g => g.Reports)
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.SetNull);

            // Indexes
            entity.HasIndex(e => e.AssignmentId);
            entity.HasIndex(e => e.GroupId);
            entity.HasIndex(e => e.SubmittedBy);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.SubmittedAt);
            entity.HasIndex(e => e.GradedBy);
            entity.HasIndex(e => new { e.AssignmentId, e.GroupId });
            entity.HasIndex(e => new { e.AssignmentId, e.SubmittedBy });
        });

        // ReportHistory configuration
        modelBuilder.Entity<ReportHistory>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Action).IsRequired();
            entity.Property(e => e.ChangedBy).HasMaxLength(450).IsRequired();
            entity.Property(e => e.ChangedAt).IsRequired();
            entity.Property(e => e.Version).IsRequired();
            entity.Property(e => e.Comment).HasMaxLength(500);

            // Relationships
            entity.HasOne(e => e.Report)
                .WithMany()
                .HasForeignKey(e => e.ReportId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes for efficient querying
            entity.HasIndex(e => e.ReportId);
            entity.HasIndex(e => e.ChangedBy);
            entity.HasIndex(e => e.ChangedAt);
            entity.HasIndex(e => new { e.ReportId, e.Version });
            entity.HasIndex(e => new { e.ReportId, e.ChangedAt });
        });

        // CrawlerChatMessage configuration
        modelBuilder.Entity<CrawlerChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ConversationId).IsRequired();
            entity.Property(e => e.AssignmentId).IsRequired();
            entity.Property(e => e.SenderId).IsRequired();
            entity.Property(e => e.MessageContent).HasMaxLength(10000).IsRequired();
            entity.Property(e => e.MessageType).IsRequired();
            entity.Property(e => e.IsSystemMessage).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.IsRead).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.CrawlResultSummary).HasMaxLength(5000);
            entity.Property(e => e.MetadataJson).HasColumnType("nvarchar(max)");

            // Relationships
            entity.HasOne(e => e.Assignment)
                .WithMany()
                .HasForeignKey(e => e.AssignmentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Group)
                .WithMany()
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            entity.HasOne(e => e.ParentMessage)
                .WithMany(m => m.Replies)
                .HasForeignKey(e => e.ParentMessageId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            // Indexes for efficient querying
            entity.HasIndex(e => e.ConversationId);
            entity.HasIndex(e => e.AssignmentId);
            entity.HasIndex(e => e.GroupId);
            entity.HasIndex(e => e.SenderId);
            entity.HasIndex(e => e.CrawlJobId);
            entity.HasIndex(e => e.ParentMessageId);
            entity.HasIndex(e => new { e.ConversationId, e.CreatedAt });
            entity.HasIndex(e => new { e.AssignmentId, e.CreatedAt });
        });

        // ReportCrawlData configuration
        modelBuilder.Entity<ReportCrawlData>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ReportId).IsRequired();
            entity.Property(e => e.CrawlJobId).IsRequired();
            entity.Property(e => e.ConversationId).IsRequired();
            entity.Property(e => e.DataSummary).HasMaxLength(5000).IsRequired();
            entity.Property(e => e.SourceUrl).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.Title).HasMaxLength(500).IsRequired();
            entity.Property(e => e.LinkedAt).IsRequired();
            entity.Property(e => e.LinkedBy).IsRequired();
            entity.Property(e => e.DisplayOrder).IsRequired().HasDefaultValue(0);
            entity.Property(e => e.IsIncludedInSubmission).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.Notes).HasMaxLength(1000);

            // Relationships
            entity.HasOne(e => e.Report)
                .WithMany()
                .HasForeignKey(e => e.ReportId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes for efficient querying
            entity.HasIndex(e => e.ReportId);
            entity.HasIndex(e => e.CrawlJobId);
            entity.HasIndex(e => e.ConversationId);
            entity.HasIndex(e => new { e.ReportId, e.DisplayOrder });
            entity.HasIndex(e => new { e.ReportId, e.IsIncludedInSubmission });
        });

        // SupportRequest configuration
        modelBuilder.Entity<SupportRequest>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Images).HasColumnType("nvarchar(max)");

            // Relationships
            entity.HasOne(e => e.Course)
                .WithMany()
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Conversation)
                .WithMany()
                .HasForeignKey(e => e.ConversationId)
                .OnDelete(DeleteBehavior.NoAction);

            // Indexes for efficient querying
            entity.HasIndex(e => e.CourseId);
            entity.HasIndex(e => e.RequesterId);
            entity.HasIndex(e => e.AssignedStaffId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Priority);
            entity.HasIndex(e => e.RequestedAt);
            entity.HasIndex(e => e.ConversationId);
        });

        // ReportAICheck configuration
        modelBuilder.Entity<ReportAICheck>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ReportId).IsRequired();
            entity.Property(e => e.AIPercentage).HasColumnType("decimal(5,2)").IsRequired();
            entity.Property(e => e.Provider).HasMaxLength(100).IsRequired();
            entity.Property(e => e.RawResponse).HasMaxLength(5000);
            entity.Property(e => e.CheckedBy).IsRequired();
            entity.Property(e => e.CheckedAt).IsRequired();
            entity.Property(e => e.Notes).HasMaxLength(1000);

            // Relationships
            entity.HasOne(e => e.Report)
                .WithMany(r => r.AIChecks)
                .HasForeignKey(e => e.ReportId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes for efficient querying
            entity.HasIndex(e => e.ReportId);
            entity.HasIndex(e => e.CheckedBy);
            entity.HasIndex(e => e.CheckedAt);
            entity.HasIndex(e => new { e.ReportId, e.CheckedAt });
        });

        // ConversationCrawlData configuration
        modelBuilder.Entity<ConversationCrawlData>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Properties
            entity.Property(e => e.SourceUrl)
                .IsRequired()
                .HasMaxLength(2000);
            
            entity.Property(e => e.NormalizedDataJson)
                .IsRequired();
            
            entity.Property(e => e.EmbeddingText)
                .IsRequired();
            
            entity.Property(e => e.DetectedSchemaJson)
                .IsRequired();
            
            // Relationships
            entity.HasOne(e => e.Conversation)
                .WithMany()
                .HasForeignKey(e => e.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Indexes for efficient querying
            entity.HasIndex(e => e.ConversationId);
            entity.HasIndex(e => e.CrawlJobId).IsUnique();
            entity.HasIndex(e => new { e.ConversationId, e.CrawledAt });
            entity.HasIndex(e => e.DataQualityScore);
        });

        // ConversationUploadedFile configuration
        modelBuilder.Entity<ConversationUploadedFile>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Properties
            entity.Property(e => e.FileName)
                .IsRequired()
                .HasMaxLength(500);
            
            entity.Property(e => e.FileUrl)
                .IsRequired()
                .HasMaxLength(2000);
            
            entity.Property(e => e.DataJson)
                .IsRequired();
            
            entity.Property(e => e.ColumnNamesJson)
                .IsRequired();
            
            entity.Property(e => e.UploadedAt)
                .IsRequired();
            
            entity.Property(e => e.UploadedBy)
                .IsRequired();
            
            entity.Property(e => e.IsDeleted)
                .IsRequired();
            
            // Relationships
            entity.HasOne(e => e.Conversation)
                .WithMany()
                .HasForeignKey(e => e.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Indexes for efficient querying
            entity.HasIndex(e => e.ConversationId);
            entity.HasIndex(e => new { e.ConversationId, e.UploadedAt });
        });

        // TemplateFile configuration
        modelBuilder.Entity<TemplateFile>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.OriginalFileName)
                .IsRequired()
                .HasMaxLength(500);
            
            entity.Property(e => e.StoredFileName)
                .IsRequired()
                .HasMaxLength(500);
            
            entity.Property(e => e.FilePath)
                .IsRequired()
                .HasMaxLength(1000);
            
            entity.Property(e => e.FileSize)
                .IsRequired();
            
            entity.Property(e => e.ContentType)
                .IsRequired()
                .HasMaxLength(200);
            
            entity.Property(e => e.UploadedBy)
                .IsRequired()
                .HasMaxLength(256);
            
            entity.Property(e => e.UploadedAt)
                .IsRequired();
            
            entity.Property(e => e.UpdatedAt)
                .IsRequired();
            
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(false);
            
            entity.Property(e => e.Description)
                .HasMaxLength(500);
            
            // Indexes
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.UploadedAt);
        });
    }
}
