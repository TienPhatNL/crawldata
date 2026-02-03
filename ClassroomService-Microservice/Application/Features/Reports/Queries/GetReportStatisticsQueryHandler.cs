using MediatR;
using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.Constants;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;

namespace ClassroomService.Application.Features.Reports.Queries;

public class GetReportStatisticsQueryHandler : IRequestHandler<GetReportStatisticsQuery, GetReportStatisticsResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetReportStatisticsQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<GetReportStatisticsResponse> Handle(GetReportStatisticsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var userRole = _currentUserService.Role;

            // Only Lecturer can access
            if (userRole != RoleConstants.Lecturer)
            {
                return new GetReportStatisticsResponse
                {
                    Success = false,
                    Message = "Access denied. Only lecturers can view report statistics."
                };
            }

            // Get basic statistics
            var (totalSubmissions, graded, pending, averageGrade) = 
                await _unitOfWork.Reports.GetReportStatisticsAsync(request.AssignmentId, cancellationToken);

            // Get all reports for detailed breakdown
            var allReports = await _unitOfWork.Reports.GetReportsByAssignmentAsync(request.AssignmentId, cancellationToken);
            
            var lateSubmissions = allReports.Count(r => r.Status == ReportStatus.Late);
            
            var statusBreakdown = allReports
                .GroupBy(r => r.Status)
                .ToDictionary(g => g.Key, g => g.Count());

            return new GetReportStatisticsResponse
            {
                Success = true,
                Message = "Report statistics retrieved successfully",
                TotalSubmissions = totalSubmissions,
                GradedCount = graded,
                PendingCount = pending,
                AverageGrade = averageGrade,
                LateSubmissions = lateSubmissions,
                StatusBreakdown = statusBreakdown
            };
        }
        catch (Exception ex)
        {
            return new GetReportStatisticsResponse
            {
                Success = false,
                Message = $"Error retrieving report statistics: {ex.Message}"
            };
        }
    }
}
