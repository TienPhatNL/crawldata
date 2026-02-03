using ClassroomService.Domain.Constants;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;
using MediatR;

namespace ClassroomService.Application.Features.Courses.Queries;

public class GetCoursesByTermAndYearQueryHandler : IRequestHandler<GetCoursesByTermAndYearQuery, GetCoursesByTermAndYearResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKafkaUserService _userService;
    private readonly ILogger<GetCoursesByTermAndYearQueryHandler> _logger;

    public GetCoursesByTermAndYearQueryHandler(
        IUnitOfWork unitOfWork,
        IKafkaUserService userService,
        ILogger<GetCoursesByTermAndYearQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _userService = userService;
        _logger = logger;
    }

    public async Task<GetCoursesByTermAndYearResponse> Handle(GetCoursesByTermAndYearQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate term exists
            var term = await _unitOfWork.Terms
                .GetByIdAsync(request.TermId, cancellationToken);

            if (term == null)
            {
                return new GetCoursesByTermAndYearResponse
                {
                    Success = false,
                    Message = "Term not found",
                    Courses = new List<CourseDto>(),
                    TotalCount = 0,
                    Page = request.Page,
                    PageSize = request.PageSize,
                    TotalPages = 0,
                    TermName = ""
                };
            }

            // Get all courses for term (with enrollments included)
            var allCourses = (await _unitOfWork.Courses.GetCoursesByTermAsync(request.TermId, cancellationToken)).ToList();

            // Apply filters in memory
            var filteredCourses = allCourses.AsEnumerable();

            // Apply status filter if provided
            if (request.Status.HasValue)
            {
                filteredCourses = filteredCourses.Where(c => c.Status == request.Status.Value);
            }

            // Apply lecturer filter if provided
            if (request.LecturerId.HasValue)
            {
                filteredCourses = filteredCourses.Where(c => c.LecturerId == request.LecturerId.Value);
            }

            // Apply course code filter if provided (null-safe)
            if (!string.IsNullOrEmpty(request.CourseCode))
            {
                filteredCourses = filteredCourses.Where(c => 
                    c.CourseCode != null && c.CourseCode.Code.Contains(request.CourseCode, StringComparison.OrdinalIgnoreCase));
            }

            // Authorization: Students can only see Active courses
            // Lecturers can see their own courses (all statuses) + Active courses
            // Staff can see all courses
            var isStaff = request.CurrentUserRole == RoleConstants.Staff;
            var isLecturer = request.CurrentUserRole == RoleConstants.Lecturer;
            var isStudent = request.CurrentUserRole == RoleConstants.Student;

            if (isStudent)
            {
                filteredCourses = filteredCourses.Where(c => c.Status == CourseStatus.Active);
            }
            else if (isLecturer && request.CurrentUserId.HasValue)
            {
                filteredCourses = filteredCourses.Where(c => c.Status == CourseStatus.Active 
                    || c.LecturerId == request.CurrentUserId.Value);
            }
            // Staff sees all courses (no additional filter)

            // Apply sorting (null-safe)
            filteredCourses = request.SortBy?.ToLower() switch
            {
                "coursecode" => request.SortDirection.ToLower() == "desc"
                    ? filteredCourses.OrderByDescending(c => c.CourseCode?.Code ?? "")
                    : filteredCourses.OrderBy(c => c.CourseCode?.Code ?? ""),
                "enrollmentcount" => request.SortDirection.ToLower() == "desc"
                    ? filteredCourses.OrderByDescending(c => c.Enrollments?.Count(e => e.Status == EnrollmentStatus.Active) ?? 0)
                    : filteredCourses.OrderBy(c => c.Enrollments?.Count(e => e.Status == EnrollmentStatus.Active) ?? 0),
                "createdat" => request.SortDirection.ToLower() == "desc"
                    ? filteredCourses.OrderByDescending(c => c.CreatedAt)
                    : filteredCourses.OrderBy(c => c.CreatedAt),
                "name" or _ => request.SortDirection.ToLower() == "desc"
                    ? filteredCourses.OrderByDescending(c => c.Name)
                    : filteredCourses.OrderBy(c => c.Name)
            };

            var filteredList = filteredCourses.ToList();
            var totalCount = filteredList.Count;

            // Apply pagination
            var courses = filteredList
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            // Get lecturer information for all courses
            var lecturerIds = courses.Select(c => c.LecturerId).Distinct().ToList();
            var lecturers = await _userService.GetUsersByIdsAsync(lecturerIds, cancellationToken);
            var lecturerDict = lecturers != null 
                ? lecturers.ToDictionary(l => l.Id, l => l) 
                : new Dictionary<Guid, UserDto>();

            // Get approver information for courses that have been approved/rejected
            var approverIds = courses
                .Where(c => c.ApprovedBy.HasValue)
                .Select(c => c.ApprovedBy!.Value)
                .Distinct()
                .ToList();
            
            var approvers = approverIds.Any() 
                ? await _userService.GetUsersByIdsAsync(approverIds, cancellationToken)
                : null;
            
            var approverDict = approvers != null
                ? approvers.ToDictionary(a => a.Id, a => a)
                : new Dictionary<Guid, UserDto>();

            // Check enrollment status for each course if user is a student
            Dictionary<Guid, bool>? enrollmentStatusDict = null;
            if (isStudent && request.CurrentUserId.HasValue)
            {
                var courseIds = courses.Select(c => c.Id).ToList();
                var studentEnrollments = await _unitOfWork.CourseEnrollments
                    .GetManyAsync(
                        e => e.StudentId == request.CurrentUserId.Value 
                            && courseIds.Contains(e.CourseId) 
                            && e.Status == EnrollmentStatus.Active,
                        cancellationToken);
                
                enrollmentStatusDict = studentEnrollments
                    .ToDictionary(e => e.CourseId, e => true);
            }

            // Build DTOs (null-safe)
            var courseDtos = courses.Select(c =>
            {
                var lecturer = lecturerDict.GetValueOrDefault(c.LecturerId);
                var lecturerName = lecturer != null
                    ? $"{lecturer.LastName} {lecturer.FirstName}".Trim()
                    : "Unknown Lecturer";
                var lecturerImage = lecturer?.ProfilePictureUrl;

                // Get approver name if approved
                string? approvedByName = null;
                if (c.ApprovedBy.HasValue)
                {
                    var approver = approverDict.GetValueOrDefault(c.ApprovedBy.Value);
                    approvedByName = approver != null
                        ? $"{approver.LastName} {approver.FirstName}".Trim()
                        : "Unknown Staff";
                }

                // Determine enrollment status for students
                bool? isEnrolled = null;
                if (enrollmentStatusDict != null)
                {
                    isEnrolled = enrollmentStatusDict.ContainsKey(c.Id);
                }

                return new CourseDto
                {
                    Id = c.Id,
                    CourseCode = c.CourseCode?.Code ?? "N/A",
                    UniqueCode = c.UniqueCode,
                    CourseCodeTitle = c.CourseCode?.Title ?? "N/A",
                    Name = c.Name,
                    Description = c.Description,
                    Term = c.Term?.Name ?? "N/A",
                    TermStartDate = c.Term?.StartDate ?? DateTime.MinValue,
                    TermEndDate = c.Term?.EndDate ?? DateTime.MinValue,
                    LecturerId = c.LecturerId,
                    LecturerName = lecturerName,
                    LecturerImage = lecturerImage,
                    Status = c.Status,
                    CreatedAt = c.CreatedAt,
                    EnrollmentCount = c.Enrollments?.Count(e => e.Status == EnrollmentStatus.Active) ?? 0,
                    ApprovedBy = c.ApprovedBy,
                    ApprovedByName = approvedByName,
                    ApprovedAt = c.ApprovedAt,
                    ApprovalComments = c.ApprovalComments,
                    RejectionReason = c.RejectionReason,
                    CanEnroll = c.Status == CourseStatus.Active,
                    RequiresAccessCode = c.RequiresAccessCode,
                    Img = c.Img,
                    Announcement = c.Announcement,
                    SyllabusFile = c.SyllabusFile,
                    Department = c.CourseCode?.Department ?? "N/A",
                    IsEnrolled = isEnrolled
                };
            }).ToList();

            var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

            _logger.LogInformation("Retrieved {Count} courses for term {TermId}", 
                courses.Count, request.TermId);

            return new GetCoursesByTermAndYearResponse
            {
                Success = true,
                Message = totalCount > 0 ? "Courses retrieved successfully" : "No courses found for the specified term",
                Courses = courseDtos,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = totalPages,
                TermName = term.Name
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving courses for term {TermId}", 
                request.TermId);
            return new GetCoursesByTermAndYearResponse
            {
                Success = false,
                Message = $"An error occurred while retrieving courses: {ex.Message}",
                Courses = new List<CourseDto>(),
                TotalCount = 0,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = 0,
                TermName = ""
            };
        }
    }
}

