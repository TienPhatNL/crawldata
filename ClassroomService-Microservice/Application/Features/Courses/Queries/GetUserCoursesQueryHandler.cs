using MediatR;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.DTOs;

namespace ClassroomService.Application.Features.Courses.Queries;

public class GetUserCoursesQueryHandler : IRequestHandler<GetUserCoursesQuery, GetUserCoursesResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKafkaUserService _userService;

    public GetUserCoursesQueryHandler(IUnitOfWork unitOfWork, IKafkaUserService userService)
    {
        _unitOfWork = unitOfWork;
        _userService = userService;
    }

    public async Task<GetUserCoursesResponse> Handle(GetUserCoursesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            List<Course> courses;

            if (request.AsLecturer)
            {
                // Get courses where user is the lecturer
                courses = (await _unitOfWork.Courses
                    .GetCoursesByLecturerIdAsync(request.UserId, cancellationToken))
                    .ToList();
            }
            else
            {
                // Get courses where user is enrolled as student with active status
                var enrollments = await _unitOfWork.CourseEnrollments
                    .GetEnrollmentsByStudentAsync(request.UserId, cancellationToken);
                
                var activeCourseIds = enrollments
                    .Where(e => e.Status == EnrollmentStatus.Active)
                    .Select(e => e.CourseId)
                    .ToList();

                courses = new List<Course>();
                foreach (var courseId in activeCourseIds)
                {
                    var course = await _unitOfWork.Courses.GetCourseWithDetailsAsync(courseId, cancellationToken);
                    if (course != null)
                    {
                        courses.Add(course);
                    }
                }
            }

            if (!courses.Any())
            {
                return new GetUserCoursesResponse
                {
                    Success = true,
                    Message = request.AsLecturer ? "No courses found for this lecturer" : "No active enrolled courses found for this student",
                    Courses = new List<CourseDto>(),
                    TotalCount = 0
                };
            }

            // Get all unique lecturer IDs
            var lecturerIds = courses.Select(c => c.LecturerId).Distinct();
            
            // Fetch lecturer information from UserService
            var lecturers = await _userService.GetUsersByIdsAsync(lecturerIds, cancellationToken);
            var lecturerDict = lecturers.ToDictionary(l => l.Id, l => l);

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
                Status = c.Status,
                CreatedAt = c.CreatedAt,
                EnrollmentCount = c.Enrollments.Count(e => e.Status == EnrollmentStatus.Active),
                RequiresAccessCode = c.RequiresAccessCode,
                Announcement = c.Announcement,
                SyllabusFile = c.SyllabusFile,
                Department = c.CourseCode.Department
            }).ToList();

            return new GetUserCoursesResponse
            {
                Success = true,
                Message = $"Successfully retrieved {courseDtos.Count} courses",
                Courses = courseDtos,
                TotalCount = courseDtos.Count
            };
        }
        catch (Exception ex)
        {
            return new GetUserCoursesResponse
            {
                Success = false,
                Message = $"Error retrieving courses: {ex.Message}",
                Courses = new List<CourseDto>(),
                TotalCount = 0
            };
        }
    }
}