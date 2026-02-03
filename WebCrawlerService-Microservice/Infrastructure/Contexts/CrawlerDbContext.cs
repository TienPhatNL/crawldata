using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using WebCrawlerService.Domain.Common;
using WebCrawlerService.Domain.Entities;
using WebCrawlerService.Domain.Enums;
using WebCrawlerService.Infrastructure.Data.SeedData;

namespace WebCrawlerService.Infrastructure.Contexts
{
    public class CrawlerDbContext : DbContext
    {
        public CrawlerDbContext(DbContextOptions<CrawlerDbContext> options) : base(options)
        {
        }

        public DbSet<CrawlJob> CrawlJobs { get; set; }
        public DbSet<CrawlerAgent> CrawlerAgents { get; set; }
        public DbSet<CrawlResult> CrawlResults { get; set; }
        public DbSet<DomainPolicy> DomainPolicies { get; set; }
        public DbSet<OutboxMessage> OutboxMessages { get; set; }
        public DbSet<CrawlTemplate> CrawlTemplates { get; set; }

        // New entities for intelligent crawling
        public DbSet<PromptHistory> PromptHistories { get; set; }
        public DbSet<NavigationStrategy> NavigationStrategies { get; set; }
        public DbSet<AgentPool> AgentPools { get; set; }
        public DbSet<AgentScalingConfig> AgentScalingConfigs { get; set; }
        public DbSet<AnalyticsCache> AnalyticsCaches { get; set; }
        public DbSet<LlmCostComparison> LlmCostComparisons { get; set; }

        // New entities for group collaboration
        public DbSet<CrawlJobParticipant> CrawlJobParticipants { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // CrawlJob configuration
            modelBuilder.Entity<CrawlJob>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Urls)
                    .HasConversion(
                        v => string.Join(';', v),
                        v => v.Split(';', StringSplitOptions.RemoveEmptyEntries))
                    .HasMaxLength(4000);
                entity.Property(e => e.ConfigurationJson).HasMaxLength(2000);
                entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
                
                entity.HasOne(e => e.AssignedAgent)
                    .WithMany(a => a.AssignedJobs)
                    .HasForeignKey(e => e.AssignedAgentId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => new { e.UserId, e.Status });
                
                // Global query filter for soft delete
                entity.HasQueryFilter(e => !e.IsDeleted);
            });

            // CrawlerAgent configuration
            modelBuilder.Entity<CrawlerAgent>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.ConfigurationJson).HasMaxLength(2000);
                entity.Property(e => e.UserAgent).HasMaxLength(500);
                
                entity.HasIndex(e => e.Type);
                entity.HasIndex(e => e.Status);
            });

            // CrawlResult configuration
            modelBuilder.Entity<CrawlResult>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Url).HasMaxLength(2000).IsRequired();
                entity.Property(e => e.ContentType).HasMaxLength(100);
                entity.Property(e => e.ContentHash).HasMaxLength(64);
                entity.Property(e => e.Title).HasMaxLength(500);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Keywords).HasMaxLength(500);
                entity.Property(e => e.Content).HasColumnType("text");
                
                entity.Property(e => e.Images)
                    .HasConversion(
                        v => string.Join(';', v),
                        v => v.Split(';', StringSplitOptions.RemoveEmptyEntries))
                    .HasMaxLength(4000);
                    
                entity.Property(e => e.Links)
                    .HasConversion(
                        v => string.Join(';', v),
                        v => v.Split(';', StringSplitOptions.RemoveEmptyEntries))
                    .HasMaxLength(4000);

                entity.HasOne(e => e.CrawlJob)
                    .WithMany(j => j.Results)
                    .HasForeignKey(e => e.CrawlJobId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired(false); // Optional to handle soft-deleted parent jobs

                entity.HasIndex(e => e.CrawlJobId);
                entity.HasIndex(e => e.Url);
                entity.HasIndex(e => e.ContentHash);
                entity.HasIndex(e => e.CrawledAt);
            });

            // DomainPolicy configuration
            modelBuilder.Entity<DomainPolicy>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DomainPattern).HasMaxLength(500).IsRequired();
                entity.Property(e => e.Reason).HasMaxLength(1000);
                
                entity.Property(e => e.AllowedRoles)
                    .HasConversion(
                        v => string.Join(',', v.Select(r => (int)r)),
                        v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                              .Select(s => (UserRole)int.Parse(s)).ToArray());

                entity.HasIndex(e => e.PolicyType);
                entity.HasIndex(e => e.IsActive);
            });

            // Configure enums as value conversions
            modelBuilder.Entity<CrawlJob>()
                .Property(e => e.Status)
                .HasConversion<int>();
                
            modelBuilder.Entity<CrawlJob>()
                .Property(e => e.Priority)
                .HasConversion<int>();
                
            modelBuilder.Entity<CrawlJob>()
                .Property(e => e.CrawlerType)
                .HasConversion<int>();

            modelBuilder.Entity<CrawlerAgent>()
                .Property(e => e.Type)
                .HasConversion<int>();
                
            modelBuilder.Entity<CrawlerAgent>()
                .Property(e => e.Status)
                .HasConversion<int>();

            modelBuilder.Entity<DomainPolicy>()
                .Property(e => e.PolicyType)
                .HasConversion<int>();
                
            modelBuilder.Entity<DomainPolicy>()
                .Property(e => e.MinimumTierRequired)
                .HasConversion<int?>();

            // CrawlTemplate configuration
            modelBuilder.Entity<CrawlTemplate>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
                entity.Property(e => e.DomainPattern).HasMaxLength(500).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.ConfigurationJson).HasColumnType("text");
                entity.Property(e => e.ApiEndpointPattern).HasMaxLength(500);
                entity.Property(e => e.LastValidationError).HasMaxLength(1000);

                entity.Property(e => e.SampleUrls)
                    .HasConversion(
                        v => string.Join(';', v),
                        v => v.Split(';', StringSplitOptions.RemoveEmptyEntries))
                    .HasMaxLength(4000);

                entity.Property(e => e.Tags)
                    .HasConversion(
                        v => string.Join(',', v),
                        v => v.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    .HasMaxLength(1000);

                entity.Property(e => e.Type).HasConversion<int>();
                entity.Property(e => e.RecommendedCrawler).HasConversion<int>();
                entity.Property(e => e.MobileApiProvider).HasConversion<int?>();
                entity.Property(e => e.MobileApiConfigJson).HasColumnType("text");

                entity.HasOne(e => e.PreviousVersion)
                    .WithMany(t => t.NewerVersions)
                    .HasForeignKey(e => e.PreviousVersionId)
                    .OnDelete(DeleteBehavior.NoAction); // Self-referencing FK - SQL Server doesn't allow cascade/set null

                entity.HasIndex(e => e.DomainPattern);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.Type);
                entity.HasIndex(e => e.CreatedBy);
                entity.HasIndex(e => e.MobileApiProvider);
            });

            // Update CrawlJob configuration for template relationship
            modelBuilder.Entity<CrawlJob>()
                .HasOne(e => e.Template)
                .WithMany(t => t.CrawlJobs)
                .HasForeignKey(e => e.TemplateId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<CrawlJob>()
                .Property(e => e.CrawlerPreference)
                .HasConversion<int>();

            modelBuilder.Entity<CrawlJob>()
                .Property(e => e.UserPrompt)
                .HasMaxLength(8000); // Increased from 2000 to accommodate enriched assignment context

            modelBuilder.Entity<CrawlJob>()
                .Property(e => e.ExtractionStrategyJson)
                .HasColumnType("text");

            // Update CrawlResult configuration for intelligent extraction fields
            modelBuilder.Entity<CrawlResult>()
                .Property(e => e.ExtractedDataJson)
                .HasColumnType("text");

            modelBuilder.Entity<CrawlResult>()
                .Property(e => e.PromptUsed)
                .HasMaxLength(2000);

            modelBuilder.Entity<CrawlResult>()
                .Property(e => e.TemplateVersion)
                .HasMaxLength(50);

            modelBuilder.Entity<CrawlResult>()
                .Property(e => e.ScreenshotBase64)
                .HasColumnType("text");

            modelBuilder.Entity<CrawlResult>()
                .Property(e => e.ExtractionWarnings)
                .HasConversion(
                    v => v != null ? string.Join(';', v) : null,
                    v => v != null ? v.Split(';', StringSplitOptions.RemoveEmptyEntries) : null)
                .HasMaxLength(2000);

            // Configure decimal precision for cost tracking
            modelBuilder.Entity<CrawlResult>()
                .Property(e => e.LlmCost)
                .HasPrecision(18, 4); // 4 decimal places for micro-transactions

            modelBuilder.Entity<CrawlResult>()
                .Property(e => e.CaptchaCost)
                .HasPrecision(18, 4); // 4 decimal places for micro-transactions

            // Configure value comparers for array properties to enable proper change tracking
            var stringArrayComparer = new ValueComparer<string[]>(
                (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
                c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c == null ? null : c.ToArray());

            var userRoleArrayComparer = new ValueComparer<UserRole[]>(
                (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
                c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c == null ? null : c.ToArray());

            // Apply comparers to CrawlJob.Urls
            modelBuilder.Entity<CrawlJob>()
                .Property(e => e.Urls)
                .Metadata.SetValueComparer(stringArrayComparer);

            // Apply comparers to CrawlResult arrays
            modelBuilder.Entity<CrawlResult>()
                .Property(e => e.Images)
                .Metadata.SetValueComparer(stringArrayComparer);

            modelBuilder.Entity<CrawlResult>()
                .Property(e => e.Links)
                .Metadata.SetValueComparer(stringArrayComparer);

            modelBuilder.Entity<CrawlResult>()
                .Property(e => e.ExtractionWarnings)
                .Metadata.SetValueComparer(stringArrayComparer);

            // Apply comparers to CrawlTemplate arrays
            modelBuilder.Entity<CrawlTemplate>()
                .Property(e => e.SampleUrls)
                .Metadata.SetValueComparer(stringArrayComparer);

            modelBuilder.Entity<CrawlTemplate>()
                .Property(e => e.Tags)
                .Metadata.SetValueComparer(stringArrayComparer);

            // Apply comparers to DomainPolicy.AllowedRoles
            modelBuilder.Entity<DomainPolicy>()
                .Property(e => e.AllowedRoles)
                .Metadata.SetValueComparer(userRoleArrayComparer);

            // OutboxMessage configuration
            modelBuilder.Entity<OutboxMessage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Type).HasMaxLength(500).IsRequired();
                entity.Property(e => e.Content).HasColumnType("text").IsRequired();
                entity.Property(e => e.Error).HasMaxLength(2000);

                entity.HasIndex(e => e.ProcessedOnUtc);
                entity.HasIndex(e => e.OccurredOnUtc);
                entity.HasIndex(e => new { e.ProcessedOnUtc, e.NextRetryAtUtc, e.RetryCount });
            });

            // PromptHistory configuration
            modelBuilder.Entity<PromptHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.PromptText).HasMaxLength(4000).IsRequired();
                entity.Property(e => e.ResponseText).HasColumnType("text");
                entity.Property(e => e.ResponseDataJson).HasColumnType("text");
                entity.Property(e => e.LlmCost).HasPrecision(18, 4);

                entity.HasOne(e => e.CrawlJob)
                    .WithMany()
                    .HasForeignKey(e => e.CrawlJobId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.Property(e => e.Type).HasConversion<int>();

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.ConversationId);
                entity.HasIndex(e => e.ProcessedAt);
                entity.HasIndex(e => new { e.UserId, e.Type });
            });

            // NavigationStrategy configuration
            modelBuilder.Entity<NavigationStrategy>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Domain).HasMaxLength(500).IsRequired();
                entity.Property(e => e.UrlPattern).HasMaxLength(1000).IsRequired();
                entity.Property(e => e.NavigationStepsJson).HasColumnType("text");
                entity.Property(e => e.Tags).HasMaxLength(500);

                entity.Property(e => e.Type).HasConversion<int>();

                entity.HasIndex(e => e.Domain);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.IsTemplate);
                entity.HasIndex(e => new { e.Domain, e.IsActive });
            });

            // AgentPool configuration
            modelBuilder.Entity<AgentPool>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.InstanceId).HasMaxLength(100).IsRequired();
                entity.Property(e => e.ContainerId).HasMaxLength(100);
                entity.Property(e => e.IpAddress).HasMaxLength(50);

                entity.Property(e => e.AgentType).HasConversion<int>();
                entity.Property(e => e.Status).HasConversion<int>();
                entity.Property(e => e.HealthStatus).HasConversion<int>();

                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.HealthStatus);
                entity.HasIndex(e => e.AgentType);
            });

            // AgentScalingConfig configuration
            modelBuilder.Entity<AgentScalingConfig>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.MaxHourlyCost).HasPrecision(18, 4);

                entity.Property(e => e.AgentType).HasConversion<int>();

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.AgentType);
                entity.HasIndex(e => e.AutoScalingEnabled);
            });

            // AnalyticsCache configuration
            modelBuilder.Entity<AnalyticsCache>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.QueryHash).HasMaxLength(64).IsRequired();
                entity.Property(e => e.OriginalQuery).HasMaxLength(2000);
                entity.Property(e => e.ResultJson).HasColumnType("text");
                entity.Property(e => e.ResultType).HasMaxLength(50);
                entity.Property(e => e.ComputationCost).HasPrecision(18, 4);

                // Convert Guid array to string for storage
                var guidArrayComparer = new ValueComparer<Guid[]>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToArray());

                entity.Property(e => e.SourceJobIds)
                    .HasConversion(
                        v => string.Join(',', v.Select(g => g.ToString())),
                        v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                              .Select(s => Guid.Parse(s)).ToArray())
                    .Metadata.SetValueComparer(guidArrayComparer);

                entity.Property(e => e.SourceJobIds).HasMaxLength(4000);

                entity.HasIndex(e => e.QueryHash).IsUnique();
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.ExpiresAt);
                entity.HasIndex(e => e.CreatedAt);
            });

            // LlmCostComparison configuration
            modelBuilder.Entity<LlmCostComparison>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Provider).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Model).HasMaxLength(100).IsRequired();
                entity.Property(e => e.InputCostPer1M).HasPrecision(18, 4);
                entity.Property(e => e.OutputCostPer1M).HasPrecision(18, 4);
                entity.Property(e => e.TotalCostUsd).HasPrecision(18, 4);

                entity.HasIndex(e => new { e.Provider, e.Model });
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.LastUsedAt);
            });

            // Update CrawlJob navigation for new entities
            modelBuilder.Entity<CrawlJob>()
                .HasOne(e => e.AssignedAgentPool)
                .WithMany()
                .HasForeignKey(e => e.AssignedAgentPoolId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<CrawlJob>()
                .HasOne(e => e.NavigationStrategy)
                .WithMany()
                .HasForeignKey(e => e.NavigationStrategyId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<CrawlJob>()
                .HasOne(e => e.ParentPrompt)
                .WithMany()
                .HasForeignKey(e => e.ParentPromptId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<CrawlJob>()
                .Property(e => e.SessionType)
                .HasConversion<int>();

            modelBuilder.Entity<CrawlJob>()
                .Property(e => e.NavigationProgressJson)
                .HasColumnType("text");

            // Seed Data
            SeedCrawlerAgents(modelBuilder);
            SeedShopeeTemplates(modelBuilder);
        }

        /// <summary>
        /// Seed crawler agents into the database
        /// </summary>
        private void SeedCrawlerAgents(ModelBuilder modelBuilder)
        {
            var agents = CrawlerAgentSeed.GetSeedAgents();
            modelBuilder.Entity<CrawlerAgent>().HasData(agents);
        }

        /// <summary>
        /// Seed Shopee crawling templates into the database
        /// </summary>
        private void SeedShopeeTemplates(ModelBuilder modelBuilder)
        {
            var templates = ShopeeTemplateSeed.GetShopeeTemplates();
            modelBuilder.Entity<CrawlTemplate>().HasData(templates);
        }
    }
}