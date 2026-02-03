using System.Text.Json;
using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Interfaces;
using MediatR;

namespace ClassroomService.Application.Features.Reports.Queries;

/// <summary>
/// Handler for getting report history
/// </summary>
public class GetReportHistoryQueryHandler : IRequestHandler<GetReportHistoryQuery, ReportHistoryResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetReportHistoryQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<ReportHistoryResponse> Handle(GetReportHistoryQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;

        // Get the report to verify access
        var report = await _unitOfWork.Reports.GetReportWithDetailsAsync(request.ReportId, cancellationToken);
        if (report == null)
        {
            throw new UnauthorizedAccessException("Report not found");
        }

        // Authorization check
        // Students: Can view history of their own reports (individual or group)
        // Lecturers: Can view history of all reports in their courses
        var currentUserRole = _currentUserService.Role;
        if (currentUserRole == "Student" && currentUserId.HasValue)
        {
            var hasAccess = false;

            // Check if student created the report
            if (report.SubmittedBy == currentUserId.Value)
            {
                hasAccess = true;
            }

            // Check if student is a group member
            if (!hasAccess && report.GroupId.HasValue)
            {
                var group = await _unitOfWork.Groups.GetGroupWithMembersAsync(report.GroupId.Value, cancellationToken);
                if (group != null)
                {
                    foreach (var member in group.Members)
                    {
                        var enrollment = await _unitOfWork.CourseEnrollments.GetAsync(
                            e => e.Id == member.EnrollmentId, cancellationToken);
                        if (enrollment != null && enrollment.StudentId == currentUserId.Value)
                        {
                            hasAccess = true;
                            break;
                        }
                    }
                }
            }

            if (!hasAccess)
            {
                throw new UnauthorizedAccessException("You don't have permission to view this report's history");
            }
        }

        // Validate and normalize pagination parameters
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        // Get history records
        var historyRecords = await _unitOfWork.ReportHistory.GetReportHistoryAsync(request.ReportId, cancellationToken);
        var totalCount = historyRecords.Count;

        // Apply pagination
        var pagedRecords = historyRecords
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Convert to DTOs
        var historyDtos = pagedRecords.Select(h =>
        {
            var dto = new ReportHistoryDto
            {
                Id = h.Id,
                ReportId = h.ReportId,
                Action = h.Action.ToString(),
                ChangedBy = h.ChangedBy,
                ChangedAt = h.ChangedAt,
                Version = h.Version,
                SequenceNumber = h.SequenceNumber,
                FullVersion = h.FullVersion,
                Comment = h.Comment,
                ChangeSummary = h.ChangeSummary,
                ChangeDetails = h.ChangeDetails,
                UnifiedDiff = h.UnifiedDiff
            };

            // Parse contributor IDs from JSON
            if (!string.IsNullOrEmpty(h.ContributorIds))
            {
                try
                {
                    var contributorIds = JsonSerializer.Deserialize<List<string>>(h.ContributorIds);
                    if (contributorIds != null && contributorIds.Any())
                    {
                        dto.ContributorIds = contributorIds;
                    }
                }
                catch
                {
                    // If JSON parsing fails, fall back to ChangedBy
                    dto.ContributorIds = new List<string> { h.ChangedBy };
                }
            }
            else
            {
                // If no ContributorIds, use ChangedBy as single contributor
                dto.ContributorIds = new List<string> { h.ChangedBy };
            }

            // Parse contributor names from Comment field (legacy support)
            if (!string.IsNullOrEmpty(h.Comment))
            {
                var contributorsMatch = System.Text.RegularExpressions.Regex.Match(
                    h.Comment, @"contributors?: (.+)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (contributorsMatch.Success)
                {
                    var contributorsPart = contributorsMatch.Groups[1].Value;
                    dto.ContributorNames = contributorsPart
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(c => c.Trim())
                        .ToList();
                }
            }

            // Parse changes from JSON
            if (!string.IsNullOrEmpty(h.OldValues) && !string.IsNullOrEmpty(h.NewValues))
            {
                try
                {
                    var oldValues = JsonSerializer.Deserialize<Dictionary<string, object>>(h.OldValues);
                    var newValues = JsonSerializer.Deserialize<Dictionary<string, object>>(h.NewValues);
                    var changes = new Dictionary<string, object>();

                    if (oldValues != null && newValues != null)
                    {
                        foreach (var key in newValues.Keys)
                        {
                            if (oldValues.ContainsKey(key))
                            {
                                changes[key] = new { Old = oldValues[key], New = newValues[key] };
                            }
                            else
                            {
                                changes[key] = new { Old = (object?)null, New = newValues[key] };
                            }
                        }
                    }

                    dto.Changes = changes;
                }
                catch
                {
                    // If JSON parsing fails, just skip changes
                }
            }

            return dto;
        }).ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new ReportHistoryResponse
        {
            ReportId = request.ReportId,
            CurrentVersion = report.Version,
            History = historyDtos,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasPrevious = pageNumber > 1,
            HasNext = pageNumber < totalPages
        };
    }
}
