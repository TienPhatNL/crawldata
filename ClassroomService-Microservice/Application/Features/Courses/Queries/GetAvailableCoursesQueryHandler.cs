using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClassroomService.Application.Features.Courses.Queries;

public class GetAvailableCoursesQueryHandler : IRequestHandler<GetAvailableCoursesQuery, GetAvailableCoursesResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKafkaUserService _userService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetAvailableCoursesQueryHandler(
        IUnitOfWork unitOfWork, 
        IKafkaUserService userService,
        IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _userService = userService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<GetAvailableCoursesResponse> Handle(GetAvailableCoursesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Get Active courses with enrollments loaded
            var activeCourses = await _unitOfWork.Courses.GetCoursesByStatusAsync(CourseStatus.Active, cancellationToken);
            var coursesList = activeCourses.ToList();

            // Apply filters in memory to ensure Enrollments are loaded
            var filteredCourses = coursesList.AsEnumerable();

            // Apply name filter
            if (!string.IsNullOrEmpty(request.Filter.Name))
            {
                filteredCourses = filteredCourses.Where(c => c.Name.Contains(request.Filter.Name, StringComparison.OrdinalIgnoreCase));
            }

            // Apply course code filter
            if (!string.IsNullOrEmpty(request.Filter.CourseCode))
            {
                filteredCourses = filteredCourses.Where(c => c.CourseCode.Code.Contains(request.Filter.CourseCode, StringComparison.OrdinalIgnoreCase));
            }

            if (request.Filter.CreatedAfter.HasValue)
            {
                filteredCourses = filteredCourses.Where(c => c.CreatedAt >= request.Filter.CreatedAfter.Value);
            }

            if (request.Filter.CreatedBefore.HasValue)
            {
                filteredCourses = filteredCourses.Where(c => c.CreatedAt <= request.Filter.CreatedBefore.Value);
            }

            // Fix: Apply enrollment count filters properly
            if (request.Filter.MinEnrollmentCount.HasValue)
            {
                filteredCourses = filteredCourses.Where(c => 
                    (c.Enrollments?.Count(e => e.Status == EnrollmentStatus.Active) ?? 0) >= request.Filter.MinEnrollmentCount.Value);
            }

            if (request.Filter.MaxEnrollmentCount.HasValue)
            {
                filteredCourses = filteredCourses.Where(c => 
                    (c.Enrollments?.Count(e => e.Status == EnrollmentStatus.Active) ?? 0) <= request.Filter.MaxEnrollmentCount.Value);
            }

            var filteredList = filteredCourses.ToList();

            // Get total count before pagination
            var totalCount = filteredList.Count;

            // Apply sorting
            var sortBy = request.Filter.SortBy?.ToLower() ?? "createdat";
            var sortDirection = request.Filter.SortDirection?.ToLower() ?? "desc";

            var sortedCourses = sortBy switch
            {
                "name" => sortDirection == "asc" 
                    ? filteredList.OrderBy(c => c.Name) 
                    : filteredList.OrderByDescending(c => c.Name),
                "coursecode" => sortDirection == "asc" 
                    ? filteredList.OrderBy(c => c.CourseCode.Code) 
                    : filteredList.OrderByDescending(c => c.CourseCode.Code),
                "enrollmentcount" => sortDirection == "asc" 
                    ? filteredList.OrderBy(c => c.Enrollments?.Count(e => e.Status == EnrollmentStatus.Active) ?? 0) 
                    : filteredList.OrderByDescending(c => c.Enrollments?.Count(e => e.Status == EnrollmentStatus.Active) ?? 0),
                _ => sortDirection == "asc" 
                    ? filteredList.OrderBy(c => c.CreatedAt) 
                    : filteredList.OrderByDescending(c => c.CreatedAt)
            };

            // Apply pagination
            var courses = sortedCourses
                .Skip((request.Filter.Page - 1) * request.Filter.PageSize)
                .Take(request.Filter.PageSize)
                .ToList();

            if (!courses.Any() && totalCount == 0)
            {
                return new GetAvailableCoursesResponse
                {
                    Success = true,
                    Message = "No courses found matching the criteria",
                    Courses = new List<AvailableCourseDto>(),
                    TotalCount = 0,
                    CurrentPage = request.Filter.Page,
                    PageSize = request.Filter.PageSize,
                    TotalPages = 0,
                    HasPreviousPage = false,
                    HasNextPage = false
                };
            }

            // Get all unique lecturer IDs
            var lecturerIds = courses.Select(c => c.LecturerId).Distinct();
            
            // Fetch lecturer information from UserService
            var lecturers = await _userService.GetUsersByIdsAsync(lecturerIds, cancellationToken);
            var lecturerDict = lecturers.ToDictionary(l => l.Id, l => l);

            // Get user enrollment statuses if user is provided
            Dictionary<Guid, UserCourseEnrollmentStatus>? userEnrollmentDict = null;
            if (request.UserId.HasValue)
            {
                var courseIds = courses.Select(c => c.Id).ToList();
                var userEnrollments = await _unitOfWork.CourseEnrollments
                    .GetEnrollmentsByStudentAsync(request.UserId.Value, cancellationToken);

                var relevantEnrollments = userEnrollments.Where(e => courseIds.Contains(e.CourseId));

                userEnrollmentDict = relevantEnrollments.ToDictionary(
                    e => e.CourseId,
                    e => new UserCourseEnrollmentStatus
                    {
                        IsEnrolled = e.Status == EnrollmentStatus.Active,
                        JoinedAt = e.Status == EnrollmentStatus.Active ? e.JoinedAt : null,
                        Status = e.Status.ToString()
                    });
            }

            // Create available course DTOs (without sensitive information)
            var availableCourseDtos = courses.Select(c => 
            {
                var lecturerName = lecturerDict.TryGetValue(c.LecturerId, out var lecturer) 
                    ? $"{lecturer.LastName} {lecturer.FirstName}".Trim()
                    : "Unknown Lecturer";

                var lecturerImage = lecturerDict.TryGetValue(c.LecturerId, out var lecturerUser)
                    ? lecturerUser.ProfilePictureUrl
                    : null;

                var userEnrollmentStatus = userEnrollmentDict?.GetValueOrDefault(c.Id);
                var isEnrolled = userEnrollmentStatus?.IsEnrolled ?? false;
                var activeEnrollmentCount = c.Enrollments?.Count(e => e.Status == EnrollmentStatus.Active) ?? 0;

                return new AvailableCourseDto
                {
                    Id = c.Id,
                    CourseCode = c.CourseCode.Code,
                    Name = c.Name,
                    Description = c.Description,
                    LecturerId = c.LecturerId,
                    LecturerName = lecturerName,
                    LecturerImage = lecturerImage,
                    CreatedAt = c.CreatedAt,
                    EnrollmentCount = activeEnrollmentCount,
                    RequiresAccessCode = c.RequiresAccessCode,
                    IsAccessCodeExpired = c.AccessCodeExpiresAt.HasValue && DateTime.UtcNow > c.AccessCodeExpiresAt,
                    Img = c.Img,
                    UniqueCode = c.UniqueCode ?? string.Empty,
                    TermName = c.Term?.Name ?? string.Empty,
                    TermStartDate = c.Term?.StartDate,
                    TermEndDate = c.Term?.EndDate,
                    EnrollmentStatus = userEnrollmentStatus,
                    CanJoin = !isEnrolled && (!c.RequiresAccessCode || !c.AccessCodeExpiresAt.HasValue || DateTime.UtcNow <= c.AccessCodeExpiresAt),
                    JoinUrl = BuildJoinUrl(c.Id)
                };
            }).ToList();

            // Apply lecturer name filter if specified (after fetching lecturer data)
            if (!string.IsNullOrEmpty(request.Filter.LecturerName))
            {
                availableCourseDtos = availableCourseDtos.Where(c => 
                    c.LecturerName.Contains(request.Filter.LecturerName, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                
                // Recalculate totals after lecturer name filter
                totalCount = availableCourseDtos.Count;
                availableCourseDtos = availableCourseDtos
                    .Skip((request.Filter.Page - 1) * request.Filter.PageSize)
                    .Take(request.Filter.PageSize)
                    .ToList();
            }

            var totalPages = (int)Math.Ceiling((double)totalCount / request.Filter.PageSize);

            return new GetAvailableCoursesResponse
            {
                Success = true,
                Message = $"Successfully retrieved {availableCourseDtos.Count} available courses (page {request.Filter.Page} of {totalPages})",
                Courses = availableCourseDtos,
                TotalCount = totalCount,
                CurrentPage = request.Filter.Page,
                PageSize = request.Filter.PageSize,
                TotalPages = totalPages,
                HasPreviousPage = request.Filter.Page > 1,
                HasNextPage = request.Filter.Page < totalPages
            };
        }
        catch (Exception ex)
        {
            return new GetAvailableCoursesResponse
            {
                Success = false,
                Message = $"Error retrieving available courses: {ex.Message}",
                Courses = new List<AvailableCourseDto>(),
                TotalCount = 0,
                CurrentPage = request.Filter.Page,
                PageSize = request.Filter.PageSize,
                TotalPages = 0,
                HasPreviousPage = false,
                HasNextPage = false
            };
        }
    }

    private string BuildJoinUrl(Guid courseId)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            var scheme = httpContext.Request.Scheme;
            var host = httpContext.Request.Host;
            return $"{scheme}://{host}/api/courses/{courseId}/enrollments/join";
        }
        
        // Fallback for when HTTP context is not available
        return $"/api/courses/{courseId}/enrollments/join";
    }
}