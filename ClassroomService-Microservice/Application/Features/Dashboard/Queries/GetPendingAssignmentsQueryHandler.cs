using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Application.Features.Dashboard.Helpers;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;
using MediatR;

namespace ClassroomService.Application.Features.Dashboard.Queries;

public class GetPendingAssignmentsQueryHandler 
    : IRequestHandler<GetPendingAssignmentsQuery, DashboardResponse<PendingAssignmentsDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly TermAccessValidator _termValidator;

    public GetPendingAssignmentsQueryHandler(
        IUnitOfWork unitOfWork, 
        ICurrentUserService currentUserService,
        TermAccessValidator termValidator)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _termValidator = termValidator;
    }

    public async Task<DashboardResponse<PendingAssignmentsDto>> Handle(
        GetPendingAssignmentsQuery request, 
        CancellationToken cancellationToken)
    {
        var studentId = _currentUserService.UserId!.Value;

        // Resolve term ID (use provided or default to current)
        var termId = request.TermId ?? await _termValidator.GetDefaultTermIdAsync(cancellationToken);

        if (!termId.HasValue)
        {
            return new DashboardResponse<PendingAssignmentsDto>
            {
                Success = false,
                Message = "No active term found",
                Data = null
            };
        }

        // Get student's enrollments
        var enrollments = await _unitOfWork.CourseEnrollments
            .GetManyAsync(e => e.StudentId == studentId && e.Status == EnrollmentStatus.Active, cancellationToken);

        var courseIds = enrollments.Select(e => e.CourseId).ToList();

        // Filter courses by term
        var courses = await _unitOfWork.Courses
            .GetManyAsync(c => courseIds.Contains(c.Id) && c.TermId == termId.Value, cancellationToken);

        var termCourseIds = courses.Select(c => c.Id).ToList();

        // Get all assignments from enrolled courses in the specified term
        var allAssignments = await _unitOfWork.Assignments
            .GetManyAsync(a => termCourseIds.Contains(a.CourseId) &&
                              (a.Status == AssignmentStatus.Active || a.Status == AssignmentStatus.Extended),
                         cancellationToken);

        // Get student's reports (includes both individual and group submissions)
        var reports = await StudentReportHelper.GetStudentReportsAsync(
            _unitOfWork,
            studentId,
            allAssignments.Select(a => a.Id),
            cancellationToken);

        var upcoming = new List<PendingAssignmentDto>();
        var drafts = new List<PendingAssignmentDto>();
        var revisions = new List<PendingAssignmentDto>();

        foreach (var assignment in allAssignments)
        {
            var report = reports.FirstOrDefault(r => r.AssignmentId == assignment.Id);
            
            // Skip if assignment is already graded
            if (report != null && report.Status == ReportStatus.Graded)
                continue;
            
            // Skip if no report and assignment is overdue or closed
            if (report == null && (assignment.Status == AssignmentStatus.Overdue || 
                                  assignment.Status == AssignmentStatus.Closed))
                continue;
            var course = await _unitOfWork.Courses.GetByIdAsync(assignment.CourseId, cancellationToken);
            var topic = await _unitOfWork.Topics.GetByIdAsync(assignment.TopicId, cancellationToken);

            var effectiveDueDate = assignment.ExtendedDueDate ?? assignment.DueDate;
            var hoursUntilDue = (int)(effectiveDueDate - DateTime.UtcNow).TotalHours;
            var isOverdue = DateTime.UtcNow > effectiveDueDate;

            var pendingDto = new PendingAssignmentDto
            {
                AssignmentId = assignment.Id,
                Title = assignment.Title,
                CourseName = course?.Name ?? "Unknown",
                TopicName = topic?.Name ?? "N/A",
                DueDate = assignment.DueDate,
                ExtendedDueDate = assignment.ExtendedDueDate,
                HoursUntilDue = hoursUntilDue,
                IsOverdue = isOverdue,
                IsGroupAssignment = assignment.IsGroupAssignment,
                GroupName = null,
                ReportStatus = report?.Status.ToString(),
                ReportId = report?.Id
            };

            // Categorize
            if (report == null)
            {
                upcoming.Add(pendingDto);
            }
            else if (report.Status == ReportStatus.Draft)
            {
                drafts.Add(pendingDto);
            }
            else if (report.Status == ReportStatus.RequiresRevision)
            {
                revisions.Add(pendingDto);
            }
        }

        var pendingAssignments = new PendingAssignmentsDto
        {
            UpcomingAssignments = upcoming.OrderBy(a => a.DueDate).ToList(),
            DraftReports = drafts.OrderBy(a => a.DueDate).ToList(),
            RevisionRequests = revisions.OrderBy(a => a.DueDate).ToList(),
            TotalPending = upcoming.Count + drafts.Count + revisions.Count
        };

        return new DashboardResponse<PendingAssignmentsDto>
        {
            Success = true,
            Data = pendingAssignments
        };
    }
}
