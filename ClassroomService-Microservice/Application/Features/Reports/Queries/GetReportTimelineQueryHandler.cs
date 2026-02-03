using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;
using MediatR;

namespace ClassroomService.Application.Features.Reports.Queries;

/// <summary>
/// Handler for getting human-readable timeline
/// </summary>
public class GetReportTimelineQueryHandler : IRequestHandler<GetReportTimelineQuery, TimelineResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetReportTimelineQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TimelineResponse> Handle(GetReportTimelineQuery request, CancellationToken cancellationToken)
    {
        var historyRecords = await _unitOfWork.ReportHistory.GetReportHistoryAsync(request.ReportId, cancellationToken);

        var timelineItems = historyRecords.Select(h =>
        {
            var item = new TimelineItemDto
            {
                Timestamp = h.ChangedAt,
                Actor = h.ChangedBy,
                Action = GetActionDescription(h.Action),
                Version = h.Version,
                Details = h.Comment
            };

            // Parse contributor IDs from JSON
            if (!string.IsNullOrEmpty(h.ContributorIds))
            {
                try
                {
                    var contributorIds = System.Text.Json.JsonSerializer.Deserialize<List<string>>(h.ContributorIds);
                    if (contributorIds != null && contributorIds.Any())
                    {
                        item.ContributorIds = contributorIds;
                    }
                }
                catch
                {
                    // If JSON parsing fails, fall back to ChangedBy
                    item.ContributorIds = new List<string> { h.ChangedBy };
                }
            }
            else
            {
                // If no ContributorIds, use ChangedBy as single contributor
                item.ContributorIds = new List<string> { h.ChangedBy };
            }

            // Extract contributor names from Comment field
            // Format: "Action description | Contributors: John Doe, Jane Smith"
            if (!string.IsNullOrEmpty(h.Comment))
            {
                var contributorsPrefix = " | Contributors: ";
                var contributorsIndex = h.Comment.IndexOf(contributorsPrefix);
                if (contributorsIndex >= 0)
                {
                    var contributorsString = h.Comment.Substring(contributorsIndex + contributorsPrefix.Length);
                    var contributors = contributorsString.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(c => c.Trim())
                        .Where(c => !string.IsNullOrEmpty(c))
                        .ToList();
                    
                    if (contributors.Any())
                    {
                        item.ContributorNames = contributors;
                    }
                }
            }

            return item;
        }).OrderBy(t => t.Timestamp).ToList();

        return new TimelineResponse
        {
            ReportId = request.ReportId,
            Timeline = timelineItems
        };
    }

    private string GetActionDescription(ReportHistoryAction action)
    {
        return action switch
        {
            ReportHistoryAction.Created => "created the draft",
            ReportHistoryAction.Updated => "edited content",
            ReportHistoryAction.Submitted => "submitted for review",
            ReportHistoryAction.Resubmitted => "resubmitted after revision",
            ReportHistoryAction.Graded => "graded the report",
            ReportHistoryAction.RevisionRequested => "requested revisions",
            ReportHistoryAction.Rejected => "rejected the report",
            ReportHistoryAction.StatusChanged => "changed status",
            ReportHistoryAction.RevertedToDraft => "reverted to draft",
            ReportHistoryAction.ContentReverted => "reverted content to previous version",
            _ => "performed an action"
        };
    }
}
