using ClosedXML.Excel;
using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Interfaces;

namespace ClassroomService.Application.Services;

public interface IGradeExportService
{
    Task<byte[]> ExportCourseGradesToExcelAsync(Guid courseId, CancellationToken cancellationToken = default);
}

public class GradeExportService : IGradeExportService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKafkaUserService _userService;

    public GradeExportService(IUnitOfWork unitOfWork, IKafkaUserService userService)
    {
        _unitOfWork = unitOfWork;
        _userService = userService;
    }

    public async Task<byte[]> ExportCourseGradesToExcelAsync(Guid courseId, CancellationToken cancellationToken = default)
    {
        var course = await _unitOfWork.Courses.GetByIdAsync(courseId, cancellationToken);
        if (course == null) throw new InvalidOperationException("Course not found");

        var courseCode = await _unitOfWork.CourseCodes.GetByIdAsync(course.CourseCodeId, cancellationToken);
        
        // Get enrollments
        var enrollments = await _unitOfWork.CourseEnrollments
            .GetManyAsync(e => e.CourseId == courseId && e.Status == Domain.Enums.EnrollmentStatus.Active, cancellationToken);

        // Get assignments
        var assignments = (await _unitOfWork.Assignments
            .GetManyAsync(a => a.CourseId == courseId, cancellationToken)).OrderBy(a => a.CreatedAt).ToList();

        // Get all reports
        var reports = await _unitOfWork.Reports
            .GetManyAsync(r => assignments.Select(a => a.Id).Contains(r.AssignmentId), cancellationToken);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add($"{courseCode?.Code ?? "Course"} Grades");

        // Header row
        int col = 1;
        worksheet.Cell(1, col++).Value = "Student ID";
        worksheet.Cell(1, col++).Value = "Student Name";
        worksheet.Cell(1, col++).Value = "Student Email";
        
        // Add assignment columns
        foreach (var assignment in assignments)
        {
            worksheet.Cell(1, col++).Value = $"{assignment.Title} (Grade)";
            worksheet.Cell(1, col++).Value = $"{assignment.Title} (Weight %)";
            worksheet.Cell(1, col++).Value = $"{assignment.Title} (Weighted)";
        }
        
        worksheet.Cell(1, col++).Value = "Weighted Total";
        worksheet.Cell(1, col++).Value = "Letter Grade";
        worksheet.Cell(1, col++).Value = "Completed";
        worksheet.Cell(1, col++).Value = "Total Assignments";

        // Style header row
        var headerRange = worksheet.Range(1, 1, 1, col - 1);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Data rows
        int row = 2;
        foreach (var enrollment in enrollments)
        {
            var student = await _userService.GetUserByIdAsync(enrollment.StudentId, cancellationToken);
            var studentReports = reports.Where(r => r.SubmittedBy == enrollment.StudentId).ToList();

            col = 1;
            worksheet.Cell(row, col++).Value = enrollment.StudentId.ToString();
            worksheet.Cell(row, col++).Value = student?.FullName ?? "Unknown";
            worksheet.Cell(row, col++).Value = student?.Email ?? "N/A";

            decimal weightedTotal = 0;
            int completedCount = 0;

            foreach (var assignment in assignments)
            {
                var report = studentReports.FirstOrDefault(r => r.AssignmentId == assignment.Id);
                var weightSnapshot = assignment.WeightPercentageSnapshot ?? 0;

                if (report?.Grade.HasValue == true)
                {
                    worksheet.Cell(row, col++).Value = report.Grade.Value;
                    worksheet.Cell(row, col++).Value = weightSnapshot;
                    var weighted = (decimal)report.Grade.Value * ((decimal)weightSnapshot / 100m);
                    worksheet.Cell(row, col++).Value = weighted;
                    weightedTotal += weighted;
                    completedCount++;
                }
                else
                {
                    worksheet.Cell(row, col++).Value = "";
                    worksheet.Cell(row, col++).Value = weightSnapshot;
                    worksheet.Cell(row, col++).Value = "";
                }
            }

            var letterGrade = CalculateLetterGrade(weightedTotal);
            
            worksheet.Cell(row, col++).Value = weightedTotal;
            worksheet.Cell(row, col++).Value = letterGrade;
            worksheet.Cell(row, col++).Value = completedCount;
            worksheet.Cell(row, col++).Value = assignments.Count;

            row++;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        // Format number columns
        for (int i = 4; i <= col - 1; i++)
        {
            if (!worksheet.Cell(1, i).Value.ToString().Contains("Letter") && 
                !worksheet.Cell(1, i).Value.ToString().Contains("Student"))
            {
                worksheet.Column(i).Style.NumberFormat.Format = "0.00";
            }
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static string CalculateLetterGrade(decimal grade)
    {
        if (grade >= 90) return "A";
        if (grade >= 80) return "B";
        if (grade >= 70) return "C";
        if (grade >= 60) return "D";
        return "F";
    }
}
