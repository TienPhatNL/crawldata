using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;
using MediatR;

namespace ClassroomService.Application.Features.Courses.Queries;

public class GetMyCoursesCommandHandler : IRequestHandler<GetMyCoursesCommand, GetMyCoursesResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKafkaUserService _userService;
    private readonly IAccessCodeService _accessCodeService;

    public GetMyCoursesCommandHandler(
        IUnitOfWork unitOfWork, 
        IKafkaUserService userService,
        IAccessCodeService accessCodeService)
    {
        _unitOfWork = unitOfWork;
        _userService = userService;
        _accessCodeService = accessCodeService;
    }

    public async Task<GetMyCoursesResponse> Handle(GetMyCoursesCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get current user information to determine role
            var currentUser = await _userService.GetUserByIdAsync(request.UserId, cancellationToken);
            var currentUserRole = currentUser?.Role;

            List<Course> courses;

            if (request.AsLecturer)
            {
                // Get courses where user is the lecturer
                var lecturerCourses = await _unitOfWork.Courses.GetManyAsync(
                    c => c.LecturerId == request.UserId,
                    cancellationToken,
                    c => c.CourseCode,
                    c => c.Term,
                    c => c.Enrollments);
                
                courses = lecturerCourses.ToList();
            }
            else
            {
                // Get courses where user is enrolled as student
                // Only show Active courses to students
                var enrollments = await _unitOfWork.CourseEnrollments.GetManyAsync(
                    e => e.StudentId == request.UserId 
                        && e.Status == EnrollmentStatus.Active
                        && e.Course.Status == CourseStatus.Active,
                    cancellationToken,
                    e => e.Course,
                    e => e.Course.CourseCode,
                    e => e.Course.Term,
                    e => e.Course.Enrollments);
                
                courses = enrollments.Select(e => e.Course).ToList();
            }

            // Apply filters (in-memory since we already have the data)
            var filteredCourses = courses.AsEnumerable();

            if (!string.IsNullOrEmpty(request.Filter.Name))
            {
                filteredCourses = filteredCourses.Where(c => c.Name.Contains(request.Filter.Name, StringComparison.OrdinalIgnoreCase));
            }

            // Apply course code filter
            if (!string.IsNullOrEmpty(request.Filter.CourseCode))
            {
                filteredCourses = filteredCourses.Where(c => c.CourseCode != null && 
                    c.CourseCode.Code.Contains(request.Filter.CourseCode, StringComparison.OrdinalIgnoreCase));
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

            filteredCourses = sortBy switch
            {
                "name" => sortDirection == "asc" ? filteredCourses.OrderBy(c => c.Name) : filteredCourses.OrderByDescending(c => c.Name),
                "coursecode" => sortDirection == "asc" ? filteredCourses.OrderBy(c => c.CourseCode?.Code) : filteredCourses.OrderByDescending(c => c.CourseCode?.Code),
                "enrollmentcount" => sortDirection == "asc" 
                    ? filteredCourses.OrderBy(c => c.Enrollments?.Count(e => e.Status == EnrollmentStatus.Active) ?? 0) 
                    : filteredCourses.OrderByDescending(c => c.Enrollments?.Count(e => e.Status == EnrollmentStatus.Active) ?? 0),
                _ => sortDirection == "asc" ? filteredCourses.OrderBy(c => c.CreatedAt) : filteredCourses.OrderByDescending(c => c.CreatedAt)
            };

            // Apply pagination
            var pagedCourses = filteredCourses
                .Skip((request.Filter.Page - 1) * request.Filter.PageSize)
                .Take(request.Filter.PageSize)
                .ToList();

            if (!pagedCourses.Any() && totalCount == 0)
            {
                return new GetMyCoursesResponse
                {
                    Success = true,
                    Message = request.AsLecturer ? "No courses found for this lecturer" : "No active enrolled courses found for this student",
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
            var lecturerIds = pagedCourses.Select(c => c.LecturerId).Distinct();
            
            // Fetch lecturer information from UserService
            var lecturers = await _userService.GetUsersByIdsAsync(lecturerIds, cancellationToken);
            var lecturerDict = lecturers.ToDictionary(l => l.Id, l => l);

            // Create course DTOs with proper access control
            var courseDtos = pagedCourses.Select(c => 
            {
                var lecturerName = lecturerDict.TryGetValue(c.LecturerId, out var lecturer) 
                    ? $"{lecturer.LastName} {lecturer.FirstName}".Trim()
                    : "Unknown Lecturer";
                var lecturerImage = lecturer?.ProfilePictureUrl;

                return CourseDtoBuilder.BuildCourseDto(
                    course: c,
                    lecturerName: lecturerName,
                    enrollmentCount: c.Enrollments?.Count(e => e.Status == EnrollmentStatus.Active) ?? 0,
                    currentUserId: request.UserId,
                    currentUserRole: currentUserRole,
                    accessCodeService: _accessCodeService,
                    showFullAccessCodeInfo: request.AsLecturer, // Show full info only for lecturer's own courses
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

            return new GetMyCoursesResponse
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
            return new GetMyCoursesResponse
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