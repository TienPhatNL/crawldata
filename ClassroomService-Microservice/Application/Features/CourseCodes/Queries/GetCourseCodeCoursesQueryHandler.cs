using ClassroomService.Application.Features.Courses.Queries;
using ClassroomService.Application.Features.Enrollments.Queries;
using ClassroomService.Domain.Constants;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClassroomService.Application.Features.CourseCodes.Queries;

public class GetCourseCodeCoursesQueryHandler : IRequestHandler<GetCourseCodeCoursesQuery, GetCourseCodeCoursesResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ClassroomDbContext _context;
    private readonly IKafkaUserService _userService;
    private readonly ILogger<GetCourseCodeCoursesQueryHandler> _logger;

    public GetCourseCodeCoursesQueryHandler(
        IUnitOfWork unitOfWork,
        ClassroomDbContext context,
        IKafkaUserService userService,
        ILogger<GetCourseCodeCoursesQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _context = context;
        _userService = userService;
        _logger = logger;
    }

    public async Task<GetCourseCodeCoursesResponse> Handle(GetCourseCodeCoursesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Retrieving courses for course code ID: {CourseCodeId}", request.CourseCodeId);

            // Check if course code exists
            var courseCode = await _unitOfWork.CourseCodes
                .GetAsync(cc => cc.Id == request.CourseCodeId, cancellationToken);

            if (courseCode == null)
            {
                _logger.LogWarning("Course code with ID {CourseCodeId} not found", request.CourseCodeId);
                return new GetCourseCodeCoursesResponse
                {
                    Success = false,
                    Message = Messages.Error.CourseCodeNotFound,
                    Courses = new List<CourseDto>(),
                    TotalCount = 0,
                    CurrentPage = request.Page,
                    PageSize = request.PageSize,
                    TotalPages = 0,
                    HasPreviousPage = false,
                    HasNextPage = false
                };
            }

            // Get all courses for this course code with necessary includes
            var query = _context.Courses
                .Include(c => c.CourseCode)
                .Include(c => c.Term)
                .Include(c => c.Enrollments)
                .Where(c => c.CourseCodeId == request.CourseCodeId)
                .AsQueryable();

            // Get total count before pagination
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply sorting
            var sortDirection = request.SortDirection?.ToLower() ?? "desc";
            query = sortDirection == "asc" 
                ? query.OrderBy(c => c.CreatedAt) 
                : query.OrderByDescending(c => c.CreatedAt);

            // Apply pagination
            var courses = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            if (!courses.Any() && totalCount == 0)
            {
                return new GetCourseCodeCoursesResponse
                {
                    Success = true,
                    Message = $"No courses found for course code {courseCode.Code}",
                    Courses = new List<CourseDto>(),
                    TotalCount = 0,
                    CurrentPage = request.Page,
                    PageSize = request.PageSize,
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

            // Create course DTOs
            var courseDtos = courses.Select(c => new CourseDto
            {
                Id = c.Id,
                CourseCode = c.CourseCode.Code,
                CourseCodeTitle = c.CourseCode.Title,
                Name = c.Name,
                    Description = c.Description,
                    Term = c.Term.Name,
                    LecturerId = c.LecturerId,
                LecturerName = lecturerDict.TryGetValue(c.LecturerId, out var lecturer) 
                    ? $"{lecturer.LastName} {lecturer.FirstName}".Trim()
                    : "Unknown Lecturer",
                CreatedAt = c.CreatedAt,
                EnrollmentCount = c.Enrollments.Count,
                RequiresAccessCode = c.RequiresAccessCode,
                Department = c.CourseCode.Department
            }).ToList();

            var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

            _logger.LogInformation("Retrieved {Count} courses for course code {Code} (page {Page} of {TotalPages})", 
                courseDtos.Count, courseCode.Code, request.Page, totalPages);

            return new GetCourseCodeCoursesResponse
            {
                Success = true,
                Message = $"Successfully retrieved {courseDtos.Count} courses for {courseCode.Code} (page {request.Page} of {totalPages})",
                Courses = courseDtos,
                TotalCount = totalCount,
                CurrentPage = request.Page,
                PageSize = request.PageSize,
                TotalPages = totalPages,
                HasPreviousPage = request.Page > 1,
                HasNextPage = request.Page < totalPages
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving courses for course code {CourseCodeId}: {ErrorMessage}", request.CourseCodeId, ex.Message);
            return new GetCourseCodeCoursesResponse
            {
                Success = false,
                Message = Messages.Helpers.FormatError(Messages.Error.CoursesRetrievalFailed, ex.Message),
                Courses = new List<CourseDto>(),
                TotalCount = 0,
                CurrentPage = request.Page,
                PageSize = request.PageSize,
                TotalPages = 0,
                HasPreviousPage = false,
                HasNextPage = false
            };
        }
    }
}