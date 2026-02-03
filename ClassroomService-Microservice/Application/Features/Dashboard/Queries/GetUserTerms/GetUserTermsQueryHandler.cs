using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Application.Features.Dashboard.DTOs;
using ClassroomService.Domain.Constants;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Interfaces;
using MediatR;

namespace ClassroomService.Application.Features.Dashboard.Queries.GetUserTerms;

public class GetUserTermsQueryHandler : IRequestHandler<GetUserTermsQuery, DashboardResponse<UserTermsDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetUserTermsQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<DashboardResponse<UserTermsDto>> Handle(GetUserTermsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId!.Value;
        var userRole = _currentUserService.Role;

        List<Guid> termIds;
        Dictionary<Guid, int> termCourseCounts;

        if (userRole == RoleConstants.Student)
        {
            // Get terms where student has enrollments
            var enrollments = await _unitOfWork.CourseEnrollments
                .GetManyAsync(e => e.StudentId == userId, cancellationToken);

            var courseIds = enrollments.Select(e => e.CourseId).Distinct().ToList();
            
            var courses = await _unitOfWork.Courses
                .GetManyAsync(c => courseIds.Contains(c.Id), cancellationToken);

            termIds = courses.Select(c => c.TermId).Distinct().ToList();
            
            // Count courses per term for this student
            termCourseCounts = courses.GroupBy(c => c.TermId)
                .ToDictionary(g => g.Key, g => g.Count());
        }
        else if (userRole == RoleConstants.Lecturer)
        {
            // Get terms where lecturer has courses
            var courses = await _unitOfWork.Courses
                .GetManyAsync(c => c.LecturerId == userId, cancellationToken);

            termIds = courses.Select(c => c.TermId).Distinct().ToList();
            
            // Count courses per term for this lecturer
            termCourseCounts = courses.GroupBy(c => c.TermId)
                .ToDictionary(g => g.Key, g => g.Count());
        }
        else
        {
            return new DashboardResponse<UserTermsDto>
            {
                Success = false,
                Message = "Invalid user role",
                Data = null
            };
        }

        if (!termIds.Any())
        {
            return new DashboardResponse<UserTermsDto>
            {
                Success = true,
                Message = "No terms found for user",
                Data = new UserTermsDto
                {
                    CurrentTermId = null,
                    Terms = new List<TermSummaryDto>()
                }
            };
        }

        // Get term details
        var terms = await _unitOfWork.Terms
            .GetManyAsync(t => termIds.Contains(t.Id), cancellationToken);

        // Find current term (active term within date range)
        var now = DateTime.UtcNow;
        var currentTerm = terms.FirstOrDefault(t => 
            t.IsActive && t.StartDate <= now && t.EndDate >= now);

        var termSummaries = terms.OrderByDescending(t => t.StartDate)
            .Select(t => new TermSummaryDto
            {
                TermId = t.Id,
                Name = t.Name,
                StartDate = t.StartDate,
                EndDate = t.EndDate,
                IsActive = t.IsActive,
                IsCurrent = currentTerm != null && t.Id == currentTerm.Id,
                CourseCount = termCourseCounts.GetValueOrDefault(t.Id, 0)
            }).ToList();

        return new DashboardResponse<UserTermsDto>
        {
            Success = true,
            Message = "Terms retrieved successfully",
            Data = new UserTermsDto
            {
                CurrentTermId = currentTerm?.Id,
                Terms = termSummaries
            }
        };
    }
}
