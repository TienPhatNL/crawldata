using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;
using MediatR;

namespace ClassroomService.Application.Features.Courses.Queries;

public class GetAllCoursesQueryHandler : IRequestHandler<GetAllCoursesQuery, GetAllCoursesResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKafkaUserService _userService;
    private readonly IAccessCodeService _accessCodeService;

    public GetAllCoursesQueryHandler(
        IUnitOfWork unitOfWork, 
        IKafkaUserService userService,
        IAccessCodeService accessCodeService)
    {
        _unitOfWork = unitOfWork;
        _userService = userService;
        _accessCodeService = accessCodeService;
    }

    public async Task<GetAllCoursesResponse> Handle(GetAllCoursesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Get all courses with navigation properties loaded
            IEnumerable<Domain.Entities.Course> courses;
            
            if (request.Filter.Status.HasValue)
            {
                courses = await _unitOfWork.Courses.GetCoursesByStatusAsync(request.Filter.Status.Value, cancellationToken);
            }
            else
            {
                courses = await _unitOfWork.Courses.GetAllAsync(
                    cancellationToken,
                    c => c.CourseCode,
                    c => c.Term,
                    c => c.Enrollments);
            }
            
            var coursesList = courses.ToList();

            // Apply filters (in-memory)
            var filteredCourses = coursesList.AsEnumerable();

            if (!string.IsNullOrEmpty(request.Filter.Name))
            {
                filteredCourses = filteredCourses.Where(c => c.Name.Contains(request.Filter.Name, StringComparison.OrdinalIgnoreCase));
            }

            // Apply course code filter
            if (!string.IsNullOrEmpty(request.Filter.CourseCode))
            {
                filteredCourses = filteredCourses.Where(c => 
                    c.CourseCode != null && c.CourseCode.Code.Contains(request.Filter.CourseCode, StringComparison.OrdinalIgnoreCase));
            }

            if (request.Filter.CreatedAfter.HasValue)
            {
                filteredCourses = filteredCourses.Where(c => c.CreatedAt >= request.Filter.CreatedAfter.Value);
            }

            if (request.Filter.CreatedBefore.HasValue)
            {
                filteredCourses = filteredCourses.Where(c => c.CreatedAt <= request.Filter.CreatedBefore.Value);
            }

            if (request.Filter.MinEnrollmentCount.HasValue)
            {
                filteredCourses = filteredCourses.Where(c => 
                    c.Enrollments != null && c.Enrollments.Count(e => e.Status == EnrollmentStatus.Active) >= request.Filter.MinEnrollmentCount.Value);
            }

            if (request.Filter.MaxEnrollmentCount.HasValue)
            {
                filteredCourses = filteredCourses.Where(c => 
                    c.Enrollments != null && c.Enrollments.Count(e => e.Status == EnrollmentStatus.Active) <= request.Filter.MaxEnrollmentCount.Value);
            }

            // Get total count before pagination
            var totalCount = filteredCourses.Count();

            // Apply sorting
            var sortBy = request.Filter.SortBy?.ToLower() ?? "createdat";
            var sortDirection = request.Filter.SortDirection?.ToLower() ?? "desc";

            // Sort the filtered courses
            var sortedCourses = sortBy switch
            {
                "name" => sortDirection == "asc" 
                    ? filteredCourses.OrderBy(c => c.Name) 
                    : filteredCourses.OrderByDescending(c => c.Name),
                "coursecode" => sortDirection == "asc" 
                    ? filteredCourses.OrderBy(c => c.CourseCode?.Code ?? string.Empty) 
                    : filteredCourses.OrderByDescending(c => c.CourseCode?.Code ?? string.Empty),
                "enrollmentcount" => sortDirection == "asc" 
                    ? filteredCourses.OrderBy(c => c.Enrollments?.Count(e => e.Status == EnrollmentStatus.Active) ?? 0) 
                    : filteredCourses.OrderByDescending(c => c.Enrollments?.Count(e => e.Status == EnrollmentStatus.Active) ?? 0),
                _ => sortDirection == "asc" 
                    ? filteredCourses.OrderBy(c => c.CreatedAt) 
                    : filteredCourses.OrderByDescending(c => c.CreatedAt)
            };

            // Apply pagination after sorting
            var paginatedCourses = sortedCourses
                .Skip((request.Filter.Page - 1) * request.Filter.PageSize)
                .Take(request.Filter.PageSize)
                .ToList();

            if (!paginatedCourses.Any() && totalCount == 0)
            {
                return new GetAllCoursesResponse
                {
                    Success = true,
                    Message = "No courses found matching the criteria",
                    Courses = new List<CourseDto>(),
                    TotalCount = 0,
                    CurrentPage = request.Filter.Page,
                    PageSize = request.Filter.PageSize,
                    TotalPages = 0,
                    HasPreviousPage = false,
                    HasNextPage = false
                };
            }

            // Get all unique lecturer IDs
            var lecturerIds = paginatedCourses.Select(c => c.LecturerId).Distinct();
            
            // Fetch lecturer information from UserService
            var lecturers = await _userService.GetUsersByIdsAsync(lecturerIds, cancellationToken);
            var lecturerDict = lecturers.ToDictionary(l => l.Id, l => l);

            // Create course DTOs with proper access control
            var courseDtos = paginatedCourses.Select(c => 
            {
                var lecturerName = lecturerDict.TryGetValue(c.LecturerId, out var lecturer) 
                    ? $"{lecturer.LastName} {lecturer.FirstName}".Trim()
                    : "Unknown Lecturer";
                var lecturerImage = lecturer?.ProfilePictureUrl;

                return CourseDtoBuilder.BuildCourseDto(
                    course: c,
                    lecturerName: lecturerName,
                    enrollmentCount: c.Enrollments?.Count(e => e.Status == EnrollmentStatus.Active) ?? 0,
                    currentUserId: request.CurrentUserId,
                    currentUserRole: request.CurrentUserRole,
                    accessCodeService: _accessCodeService,
                    showFullAccessCodeInfo: false, // Never show full access code info in general admin view
                    lecturerImage: lecturerImage
                );
            }).ToList();

            // Apply lecturer name filter if specified
            if (!string.IsNullOrEmpty(request.Filter.LecturerName))
            {
                courseDtos = courseDtos.Where(c => 
                    c.LecturerName.Contains(request.Filter.LecturerName, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                
                // Recalculate totals after lecturer name filter
                totalCount = courseDtos.Count;
                courseDtos = courseDtos
                    .Skip((request.Filter.Page - 1) * request.Filter.PageSize)
                    .Take(request.Filter.PageSize)
                    .ToList();
            }

            var totalPages = (int)Math.Ceiling((double)totalCount / request.Filter.PageSize);

            return new GetAllCoursesResponse
            {
                Success = true,
                Message = $"Successfully retrieved {courseDtos.Count} courses (page {request.Filter.Page} of {totalPages})",
                Courses = courseDtos,
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
            return new GetAllCoursesResponse
            {
                Success = false,
                Message = $"Error retrieving courses: {ex.Message}",
                Courses = new List<CourseDto>(),
                TotalCount = 0,
                CurrentPage = request.Filter.Page,
                PageSize = request.Filter.PageSize,
                TotalPages = 0,
                HasPreviousPage = false,
                HasNextPage = false
            };
        }
    }
}