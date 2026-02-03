using System.Text.Json;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Interfaces;
using MediatR;

namespace ClassroomService.Application.Features.Reports.Queries;

/// <summary>
/// Handler for getting detailed information about a specific version
/// </summary>
public class GetVersionDetailQueryHandler : IRequestHandler<GetVersionDetailQuery, ReportHistoryDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetVersionDetailQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ReportHistoryDto> Handle(GetVersionDetailQuery request, CancellationToken cancellationToken)
    {
        var historyRecord = await _unitOfWork.ReportHistory.GetVersionAsync(request.ReportId, request.Version, cancellationToken);
        
        if (historyRecord == null)
        {
            throw new KeyNotFoundException($"Version {request.Version} not found for report {request.ReportId}");
        }

        var dto = new ReportHistoryDto
        {
            Id = historyRecord.Id,
            ReportId = historyRecord.ReportId,
            Action = historyRecord.Action.ToString(),
            ChangedBy = historyRecord.ChangedBy,
            ChangedAt = historyRecord.ChangedAt,
            Version = historyRecord.Version,
            Comment = historyRecord.Comment,
            ChangeSummary = historyRecord.ChangeSummary,
            ChangeDetails = historyRecord.ChangeDetails,
            UnifiedDiff = historyRecord.UnifiedDiff
        };

        // Parse contributor names from Comment field
        if (!string.IsNullOrEmpty(historyRecord.Comment))
        {
            var contributorsMatch = System.Text.RegularExpressions.Regex.Match(
                historyRecord.Comment, @"contributors?: (.+)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
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
        if (!string.IsNullOrEmpty(historyRecord.OldValues) && !string.IsNullOrEmpty(historyRecord.NewValues))
        {
            try
            {
                var oldValues = JsonSerializer.Deserialize<Dictionary<string, object>>(historyRecord.OldValues);
                var newValues = JsonSerializer.Deserialize<Dictionary<string, object>>(historyRecord.NewValues);
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
    }
}
