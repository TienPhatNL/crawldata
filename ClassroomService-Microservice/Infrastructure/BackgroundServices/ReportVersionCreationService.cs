using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Enums;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ClassroomService.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that periodically checks report buffers and creates versions
/// when debounce period (60 seconds) has elapsed since last activity
/// </summary>
public class ReportVersionCreationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReportVersionCreationService> _logger;
    private readonly TimeSpan _checkInterval;

    public ReportVersionCreationService(
        IServiceScopeFactory scopeFactory,
        ILogger<ReportVersionCreationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _checkInterval = TimeSpan.FromSeconds(10); // Check every 10 seconds
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 Report Version Creation Service started - checking every {Seconds} seconds", 
            _checkInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("⏰ Running background check for pending versions...");
                await ProcessPendingVersions(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in Report Version Creation Service");
            }

            // Wait for next interval
            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("🛑 Report Version Creation Service stopped");
    }

    private async Task ProcessPendingVersions(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var bufferService = scope.ServiceProvider.GetRequiredService<IReportCollaborationBufferService>();
        var reportRepository = scope.ServiceProvider.GetRequiredService<IReportRepository>();
        var reportHistoryRepository = scope.ServiceProvider.GetRequiredService<IReportHistoryRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var changeTrackingService = scope.ServiceProvider.GetRequiredService<IChangeTrackingService>();

        try
        {
            // Get all active report sessions
            var activeSessions = await bufferService.GetAllActiveSessionsAsync();

            _logger.LogInformation("📊 Found {Count} active report sessions to check", activeSessions.Count);

            if (!activeSessions.Any())
            {
                _logger.LogInformation("💤 No active sessions - skipping version creation");
                return;
            }

            foreach (var sessionReportId in activeSessions)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                _logger.LogInformation("🔍 Checking report {ReportId} for pending changes...", sessionReportId);

                await ProcessReportVersion(
                    sessionReportId, 
                    bufferService, 
                    reportRepository, 
                    reportHistoryRepository,
                    unitOfWork,
                    changeTrackingService,
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing pending versions");
        }
    }

    private async Task ProcessReportVersion(
        Guid reportId,
        IReportCollaborationBufferService bufferService,
        IReportRepository reportRepository,
        IReportHistoryRepository reportHistoryRepository,
        IUnitOfWork unitOfWork,
        IChangeTrackingService changeTrackingService,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get inactivity info for debugging
            var lastActivity = await bufferService.GetLastActivityAsync(reportId, cancellationToken);
            var pendingCount = await bufferService.GetPendingChangesCountAsync(reportId, cancellationToken);
            
            _logger.LogInformation("📋 Report {ReportId}: LastActivity={LastActivity}, PendingChanges={PendingCount}", 
                reportId, lastActivity?.ToString("HH:mm:ss") ?? "null", pendingCount);

            // Check if buffer should be flushed (60s inactivity OR size/time limits)
            var shouldFlush = await bufferService.ShouldFlushBufferAsync(reportId);
            if (!shouldFlush)
            {
                _logger.LogInformation("⏸️ Report {ReportId}: Not ready to flush yet", reportId);
                return;
            }

            _logger.LogInformation("💾 Creating version for report {ReportId}", reportId);

            // Get buffered changes
            var changes = await bufferService.GetBufferedChangesAsync(reportId);
            if (changes == null || !changes.Any())
            {
                _logger.LogWarning("⚠️ No buffered changes found for report {ReportId}, skipping version creation", reportId);
                await bufferService.ClearBufferAsync(reportId);
                return;
            }

            // Get session info (contributors, activity timestamps)
            var sessionInfo = await bufferService.GetSessionInfoAsync(reportId);

            // Get current report state
            var report = await reportRepository.GetByIdAsync(reportId, cancellationToken);
            if (report == null)
            {
                _logger.LogWarning("⚠️ Report {ReportId} not found in database, clearing buffer", reportId);
                await bufferService.ClearBufferAsync(reportId);
                return;
            }

            _logger.LogInformation("✅ Report {ReportId} found, proceeding with version creation", reportId);

            // Calculate metrics
            var firstChange = changes.First();
            var lastChange = changes.Last();
            var editDuration = (lastChange.Timestamp - firstChange.Timestamp).TotalMinutes;
            var charactersAdded = changes.Sum(c => c.ChangeType == "insert" ? c.Content?.Length ?? 0 : 0);
            var charactersDeleted = changes.Sum(c => c.ChangeType == "delete" ? c.Content?.Length ?? 0 : 0);

            // Get the latest content from buffer
            var newContent = await bufferService.GetLatestWorkingContentAsync(reportId);
            if (string.IsNullOrEmpty(newContent))
            {
                // Fallback to last change if buffer is empty
                newContent = lastChange.Content;
            }
            var oldContent = report.Submission;

            // Update the report with the new content
            report.Submission = newContent ?? string.Empty;
            report.Version += 1;
            report.UpdatedAt = lastChange.Timestamp;
            await reportRepository.UpdateAsync(report, cancellationToken);
            
            // CRITICAL: Save changes to database!
            await unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("📝 Updated report {ReportId} to version {Version} with new content (length: {Length})", 
                reportId, report.Version, newContent?.Length ?? 0);

            // Generate batch ID to group this set of changes
            var batchId = Guid.NewGuid().ToString();

            // Get contributor names for comment
            var contributorNames = sessionInfo?.ActiveUsers.Select(u => u.UserName).ToList() ?? new List<string>();
            var contributorList = contributorNames.Any() ? string.Join(", ", contributorNames) : "Unknown";

            // 🆕 Calculate detailed diff using change tracking service
            var diffResult = changeTrackingService.CalculateDiff(oldContent, newContent);
            var changeSummary = changeTrackingService.GenerateSummary(diffResult);
            var changeDetails = changeTrackingService.SerializeChangeOperations(diffResult);
            var unifiedDiff = changeTrackingService.CreateUnifiedDiff(oldContent, newContent);

            _logger.LogInformation("📊 Diff calculated: {Summary}", changeSummary);

            // Create a single version entry representing the batch
            var reportHistory = new ReportHistory
            {
                ReportId = reportId,
                Action = ReportHistoryAction.Updated,
                ChangedAt = lastChange.Timestamp,
                ChangedBy = (sessionInfo?.ActiveUsers.FirstOrDefault()?.UserId ?? Guid.Empty).ToString(),
                Version = report.Version,
                Comment = $"Collaborative edit: {changes.Count} changes from contributors: {contributorList}",
                OldValues = System.Text.Json.JsonSerializer.Serialize(new { 
                    Submission = oldContent,
                    Status = report.Status.ToString()
                }),
                NewValues = System.Text.Json.JsonSerializer.Serialize(new { 
                    Submission = newContent,
                    Status = report.Status.ToString()
                }),
                
                // Collaboration-specific fields
                ContributorIds = System.Text.Json.JsonSerializer.Serialize(
                    sessionInfo?.ActiveUsers.Select(u => u.UserId).Distinct().ToList() ?? new List<Guid>()
                ),
                BatchId = Guid.Parse(batchId),
                IsBatchFlush = true,
                CharactersAdded = charactersAdded,
                CharactersDeleted = charactersDeleted,
                EditDuration = TimeSpan.FromMinutes(editDuration),
                
                // 🆕 NEW: Detailed change tracking
                ChangeDetails = changeDetails,
                ChangeSummary = changeSummary,
                UnifiedDiff = unifiedDiff
            };

            // Save version to database
            await reportHistoryRepository.AddAsync(reportHistory, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            // Clear the buffer
            await bufferService.ClearBufferAsync(reportId);

            _logger.LogInformation(
                "✅ Created version {VersionId} for report {ReportId} with {ChangeCount} changes, " +
                "contributors: [{Contributors}], {CharsAdded} chars added, {CharsDeleted} chars deleted, " +
                "{Duration:F2} minutes duration",
                reportHistory.Id, reportId, changes.Count, contributorList,
                charactersAdded, charactersDeleted, editDuration);

            // Optionally notify connected clients about version creation
            // (You can inject IHubContext<ReportCollaborationHub> if needed)
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating version for report {ReportId}", reportId);
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Report Version Creation Service is stopping");
        return base.StopAsync(cancellationToken);
    }
}
