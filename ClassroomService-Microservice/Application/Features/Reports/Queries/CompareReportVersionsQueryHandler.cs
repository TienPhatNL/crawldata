using System.Text.Json;
using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ClassroomService.Application.Features.Reports.Queries;

/// <summary>
/// Handler for comparing aggregate report versions
/// Compares all sequences within each version to show the complete evolution
/// </summary>
public class CompareReportVersionsQueryHandler : IRequestHandler<CompareReportVersionsQuery, CompareVersionsResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IChangeTrackingService _changeTrackingService;
    private readonly IKafkaUserService _userService;
    private readonly ILogger<CompareReportVersionsQueryHandler> _logger;

    public CompareReportVersionsQueryHandler(
        IUnitOfWork unitOfWork,
        IChangeTrackingService changeTrackingService,
        IKafkaUserService userService,
        ILogger<CompareReportVersionsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _changeTrackingService = changeTrackingService;
        _userService = userService;
        _logger = logger;
    }

    public async Task<CompareVersionsResponse> Handle(CompareReportVersionsQuery request, CancellationToken cancellationToken)
    {
        // Get all sequences for both versions
        var version1Sequences = await _unitOfWork.ReportHistory.GetAllSequencesForVersionAsync(
            request.ReportId, request.Version1, cancellationToken);
        var version2Sequences = await _unitOfWork.ReportHistory.GetAllSequencesForVersionAsync(
            request.ReportId, request.Version2, cancellationToken);

        if (!version1Sequences.Any() || !version2Sequences.Any())
        {
            throw new ArgumentException("One or both versions not found");
        }

        // Build aggregated version DTOs
        var aggregated1 = BuildAggregatedVersion(version1Sequences);
        var aggregated2 = BuildAggregatedVersion(version2Sequences);

        // Fetch contributor names for version 1
        var contributorNames1 = new List<string>();
        foreach (var contributorId in aggregated1.Contributors)
        {
            if (Guid.TryParse(contributorId, out var userId))
            {
                var user = await _userService.GetUserByIdAsync(userId, cancellationToken);
                contributorNames1.Add(user?.FullName ?? "Unknown");
            }
            else
            {
                contributorNames1.Add("Unknown");
            }
        }
        aggregated1.ContributorNames = contributorNames1;

        // Fetch contributor names for version 2
        var contributorNames2 = new List<string>();
        foreach (var contributorId in aggregated2.Contributors)
        {
            if (Guid.TryParse(contributorId, out var userId))
            {
                var user = await _userService.GetUserByIdAsync(userId, cancellationToken);
                contributorNames2.Add(user?.FullName ?? "Unknown");
            }
            else
            {
                contributorNames2.Add("Unknown");
            }
        }
        aggregated2.ContributorNames = contributorNames2;

        // Calculate differences between final states
        var differences = CalculateDifferences(aggregated1, aggregated2);

        // Calculate diff on content
        var diffResult = _changeTrackingService.CalculateDiff(
            aggregated1.FinalContent ?? "", 
            aggregated2.FinalContent ?? "");
        var changeSummary = _changeTrackingService.GenerateSummary(diffResult);
        var unifiedDiff = _changeTrackingService.CreateUnifiedDiff(
            aggregated1.FinalContent ?? "", 
            aggregated2.FinalContent ?? "");

        // Get all unique contributor names across both versions
        var allContributorNames = contributorNames1.Concat(contributorNames2)
            .Distinct()
            .ToList();

        return new CompareVersionsResponse
        {
            ReportId = request.ReportId,
            Mode = "Version",
            AggregatedVersion1 = aggregated1,
            AggregatedVersion2 = aggregated2,
            Differences = differences,
            UnifiedDiff = unifiedDiff,
            ChangeSummary = changeSummary,
            ContributorNames = allContributorNames
        };
    }

    private AggregatedVersionDto BuildAggregatedVersion(List<ReportHistory> sequences)
    {
        var first = sequences.First();
        var last = sequences.Last();
        
        // Accumulate state across all sequences to get final values
        string? finalContent = null;
        string? finalStatus = null;
        decimal? finalGrade = null;
        string? finalFeedback = null;

        foreach (var seq in sequences)
        {
            var newData = ParseVersionData(seq.NewValues);
            var oldData = ParseVersionData(seq.OldValues);
            
            // Update state when fields change
            if (newData.ContainsKey("Submission"))
            {
                finalContent = newData.GetValueOrDefault("Submission")?.ToString();
            }
            else if (oldData.ContainsKey("Submission") && finalContent == null)
            {
                finalContent = oldData.GetValueOrDefault("Submission")?.ToString();
            }
            
            if (newData.ContainsKey("Status"))
            {
                finalStatus = newData.GetValueOrDefault("Status")?.ToString();
            }
            else if (oldData.ContainsKey("Status") && finalStatus == null)
            {
                finalStatus = oldData.GetValueOrDefault("Status")?.ToString();
            }
            
            if (newData.ContainsKey("Grade"))
            {
                decimal.TryParse(newData.GetValueOrDefault("Grade")?.ToString(), out var grade);
                finalGrade = grade;
            }
            else if (oldData.ContainsKey("Grade") && finalGrade == null)
            {
                if (decimal.TryParse(oldData.GetValueOrDefault("Grade")?.ToString(), out var oldGrade))
                {
                    finalGrade = oldGrade;
                }
            }
            
            if (newData.ContainsKey("Feedback"))
            {
                finalFeedback = newData.GetValueOrDefault("Feedback")?.ToString();
            }
            else if (oldData.ContainsKey("Feedback") && finalFeedback == null)
            {
                finalFeedback = oldData.GetValueOrDefault("Feedback")?.ToString();
            }
        }

        return new AggregatedVersionDto
        {
            Version = first.Version,
            FullVersionRange = sequences.Count == 1 
                ? first.FullVersion 
                : $"{first.FullVersion}-{last.FullVersion}",
            SequenceCount = sequences.Count,
            FinalContent = finalContent,
            FinalStatus = finalStatus,
            FinalGrade = finalGrade,
            FinalFeedback = finalFeedback,
            ActionsPerformed = sequences.Select(s => s.Action.ToString()).Distinct().ToList(),
            Contributors = sequences.Select(s => s.ChangedBy).Distinct().ToList(),
            FirstChangeAt = first.ChangedAt,
            LastChangeAt = last.ChangedAt
        };
    }

    private List<FieldDifferenceDto> CalculateDifferences(AggregatedVersionDto v1, AggregatedVersionDto v2)
    {
        var differences = new List<FieldDifferenceDto>();

        // Content
        if (v1.FinalContent != v2.FinalContent)
        {
            differences.Add(new FieldDifferenceDto
            {
                Field = "Submission",
                Changed = true,
                OldValue = v1.FinalContent,
                NewValue = v2.FinalContent
            });
        }

        // Status
        if (v1.FinalStatus != v2.FinalStatus)
        {
            differences.Add(new FieldDifferenceDto
            {
                Field = "Status",
                Changed = true,
                OldValue = v1.FinalStatus,
                NewValue = v2.FinalStatus
            });
        }

        // Grade
        if (v1.FinalGrade != v2.FinalGrade)
        {
            differences.Add(new FieldDifferenceDto
            {
                Field = "Grade",
                Changed = true,
                OldValue = v1.FinalGrade?.ToString() ?? "null",
                NewValue = v2.FinalGrade?.ToString() ?? "null"
            });
        }

        // Feedback
        if (v1.FinalFeedback != v2.FinalFeedback)
        {
            differences.Add(new FieldDifferenceDto
            {
                Field = "Feedback",
                Changed = true,
                OldValue = v1.FinalFeedback,
                NewValue = v2.FinalFeedback
            });
        }

        return differences;
    }

    private Dictionary<string, object> ParseVersionData(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return new Dictionary<string, object>();
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }
}
