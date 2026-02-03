using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace ClassroomService.Infrastructure.Services;

public class ExcelService : IExcelService
{
    private readonly ILogger<ExcelService> _logger;

    public ExcelService(ILogger<ExcelService> logger)
    {
        _logger = logger;
        // Set EPPlus license context
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    public async Task<List<ImportStudentEnrollmentDto>> ImportStudentEnrollmentsFromExcelAsync(Stream excelStream)
    {
        try
        {
            var enrollments = new List<ImportStudentEnrollmentDto>();

            using var package = new ExcelPackage(excelStream);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
            {
                throw new ArgumentException("Excel file does not contain any worksheets");
            }

            // Validate headers
            var expectedHeaders = new[] { "Student Email", "Student ID", "First Name", "Last Name", "Profile Picture URL", "Course Code", "Course Name", "Term Name" };
            for (int col = 1; col <= expectedHeaders.Length; col++)
            {
                var headerValue = worksheet.Cells[1, col].Value?.ToString() ?? "";
                if (!headerValue.Equals(expectedHeaders[col - 1], StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException($"Invalid header in column {col}. Expected '{expectedHeaders[col - 1]}' but found '{headerValue}'");
                }
            }

            // Read data rows
            var rowCount = worksheet.Dimension?.Rows ?? 0;
            for (int row = 2; row <= rowCount; row++) // Start from row 2 (skip header)
            {
                // Skip empty rows
                if (IsRowEmpty(worksheet, row, expectedHeaders.Length))
                    continue;

                var enrollment = new ImportStudentEnrollmentDto
                {
                    StudentEmail = worksheet.Cells[row, 1].Value?.ToString()?.Trim() ?? "",
                    StudentId = worksheet.Cells[row, 2].Value?.ToString()?.Trim() ?? "",
                    FirstName = worksheet.Cells[row, 3].Value?.ToString()?.Trim() ?? "",
                    LastName = worksheet.Cells[row, 4].Value?.ToString()?.Trim() ?? "",
                    ProfilePictureUrl = worksheet.Cells[row, 5].Value?.ToString()?.Trim(),
                    CourseCode = worksheet.Cells[row, 6].Value?.ToString()?.Trim() ?? "",
                    CourseName = worksheet.Cells[row, 7].Value?.ToString()?.Trim() ?? "",
                    Term = worksheet.Cells[row, 8].Value?.ToString()?.Trim() ?? "",
                    RowNumber = row
                };

                enrollments.Add(enrollment);
            }

            return await Task.FromResult(enrollments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing student enrollments from Excel");
            throw;
        }
    }

    public byte[] GenerateStudentEnrollmentTemplate()
    {
        try
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Student Enrollment Template");

            // Define headers
            var headers = new[]
            {
                "Student Email", "Student ID", "First Name", "Last Name", "Profile Picture URL", "Course Code", "Course Name", "Term Name"
            };

            // Add headers with styling
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cells[1, i + 1];
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            // Add sample data
            worksheet.Cells[2, 1].Value = "bui.thi.h@fpt.edu.vn";
            worksheet.Cells[2, 2].Value = "SE176678";
            worksheet.Cells[2, 3].Value = "Bùi";
            worksheet.Cells[2, 4].Value = "Thị H";
            worksheet.Cells[2, 5].Value = "https://example.com/profile1.jpg";
            worksheet.Cells[2, 6].Value = "CS101";
            worksheet.Cells[2, 7].Value = "CS101#A1B2C3 - Nguyễn Văn A";
            worksheet.Cells[2, 8].Value = "Spring 2025";

            worksheet.Cells[3, 1].Value = "do.van.i@fpt.edu.vn";
            worksheet.Cells[3, 2].Value = "SE177901";
            worksheet.Cells[3, 3].Value = "Đỗ";
            worksheet.Cells[3, 4].Value = "Văn I";
            worksheet.Cells[3, 5].Value = "https://example.com/profile2.jpg";
            worksheet.Cells[3, 6].Value = "MATH201";
            worksheet.Cells[3, 7].Value = "MATH201#D4E5F6 - Trần Thị B";
            worksheet.Cells[3, 8].Value = "Fall 2025";

            // Apply borders to sample data
            for (int row = 2; row <= 3; row++)
            {
                for (int col = 1; col <= headers.Length; col++)
                {
                    worksheet.Cells[row, col].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }
            }

            // Add instructions
            worksheet.Cells[5, 1].Value = "Instructions:";
            worksheet.Cells[5, 1].Style.Font.Bold = true;
            worksheet.Cells[6, 1].Value = "1. Student Email: Valid email of the student (required for all operations)";
            worksheet.Cells[7, 1].Value = "2. Student ID: Student ID that matches the email (e.g., 'STU001', 'STU002')";
            worksheet.Cells[8, 1].Value = "3. First Name: Student's first name (required for validation or auto-creation)";
            worksheet.Cells[9, 1].Value = "4. Last Name: Student's last name (required for validation or auto-creation)";
            worksheet.Cells[10, 1].Value = "5. Profile Picture URL: Optional profile picture URL";
            worksheet.Cells[11, 1].Value = "6. Course Code: Course code (e.g., 'CS101', 'MATH201')";
            worksheet.Cells[12, 1].Value = "7. Course Name: Full course name including unique code (e.g., 'CS101#A1B2C3 - Smith John')";
            worksheet.Cells[13, 1].Value = "8. Term Name: Term name (e.g., 'Spring', 'Fall', 'Q1', 'Summer') - Must exist and be active. Year is inferred from Term dates.";
            worksheet.Cells[14, 1].Value = "9. Remove the sample data rows before importing";
            worksheet.Cells[15, 1].Value = "10. AUTO-CREATION: If student not found and email domain is allowed (e.g., .edu), account will be created";
            worksheet.Cells[16, 1].Value = "11. For existing students, First/Last name must match system records";
            worksheet.Cells[17, 1].Value = "12. Lecturers can only enroll students in their own courses";
            worksheet.Cells[18, 1].Value = "13. Staff can enroll students in any course";
            worksheet.Cells[19, 1].Value = "14. Course Name must match exactly (format: CourseCode#UniqueCode - Lecturer Name)";
            worksheet.Cells[20, 1].Value = "15. The UniqueCode in Course Name ensures you're enrolling in the correct section";
            worksheet.Cells[21, 1].Value = "16. Term Name must match exactly an active term in the system";

            // Auto-fit columns
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            // Freeze header row
            worksheet.View.FreezePanes(2, 1);

            return package.GetAsByteArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating student enrollment template");
            throw;
        }
    }

    public async Task<List<ImportCourseStudentsDto>> ImportCourseStudentsFromExcelAsync(Stream excelStream)
    {
        try
        {
            var students = new List<ImportCourseStudentsDto>();

            using var package = new ExcelPackage(excelStream);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
            {
                throw new ArgumentException("Excel file does not contain any worksheets");
            }

            // Validate headers (assuming first row contains headers)
            var expectedHeaders = new[] { "Email", "Student ID", "First Name", "Last Name", "Profile Picture URL" };
            for (int col = 1; col <= expectedHeaders.Length; col++)
            {
                var headerValue = worksheet.Cells[1, col].Value?.ToString() ?? "";
                if (!headerValue.Equals(expectedHeaders[col - 1], StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException($"Invalid header in column {col}. Expected '{expectedHeaders[col - 1]}' but found '{headerValue}'");
                }
            }

            // Read data rows
            var rowCount = worksheet.Dimension?.Rows ?? 0;
            for (int row = 2; row <= rowCount; row++) // Start from row 2 (skip header)
            {
                // Skip empty rows
                if (IsRowEmpty(worksheet, row, expectedHeaders.Length))
                    continue;

                var student = new ImportCourseStudentsDto
                {
                    Email = worksheet.Cells[row, 1].Value?.ToString()?.Trim() ?? "",
                    StudentId = worksheet.Cells[row, 2].Value?.ToString()?.Trim() ?? "",
                    FirstName = worksheet.Cells[row, 3].Value?.ToString()?.Trim() ?? "",
                    LastName = worksheet.Cells[row, 4].Value?.ToString()?.Trim() ?? "",
                    ProfilePictureUrl = worksheet.Cells[row, 5].Value?.ToString()?.Trim(),
                    RowNumber = row
                };

                students.Add(student);
            }

            return await Task.FromResult(students);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing course students from Excel");
            throw;
        }
    }

    public byte[] GenerateCourseStudentsTemplate()
    {
        try
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Course Students Template");

            // Define headers
            var headers = new[]
            {
                "Email", "Student ID", "First Name", "Last Name", "Profile Picture URL"
            };

            // Add headers with styling
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cells[1, i + 1];
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            // Add sample data
            worksheet.Cells[2, 1].Value = "bui.thi.h@fpt.edu.vn";
            worksheet.Cells[2, 2].Value = "SE176678";
            worksheet.Cells[2, 3].Value = "Bùi";
            worksheet.Cells[2, 4].Value = "Thị H";
            worksheet.Cells[2, 5].Value = "https://example.com/profile1.jpg";

            worksheet.Cells[3, 1].Value = "do.van.i@fpt.edu.vn";
            worksheet.Cells[3, 2].Value = "SE177901";
            worksheet.Cells[3, 3].Value = "Đỗ";
            worksheet.Cells[3, 4].Value = "Văn I";
            worksheet.Cells[3, 5].Value = "https://example.com/profile2.jpg";

            // Apply borders to sample data
            for (int row = 2; row <= 3; row++)
            {
                for (int col = 1; col <= headers.Length; col++)
                {
                    worksheet.Cells[row, col].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }
            }

            // Add instructions
            worksheet.Cells[5, 1].Value = "Instructions:";
            worksheet.Cells[5, 1].Style.Font.Bold = true;
            worksheet.Cells[6, 1].Value = "1. Email: Valid email of the student in the system";
            worksheet.Cells[7, 1].Value = "2. Student ID: Student ID that matches the email (e.g., 'STU001', 'STU002')";
            worksheet.Cells[8, 1].Value = "3. First Name: Must match the first name in the system (or provided for auto-creation)";
            worksheet.Cells[9, 1].Value = "4. Last Name: Must match the last name in the system (or provided for auto-creation)";
            worksheet.Cells[10, 1].Value = "5. Profile Picture URL: Must match the profile picture URL in the system (optional)";
            worksheet.Cells[11, 1].Value = "6. Remove the sample data rows before importing";
            worksheet.Cells[12, 1].Value = "7. AUTO-CREATION: If student not found and email domain is allowed (e.g., .edu), account will be created";
            worksheet.Cells[13, 1].Value = "8. For existing students, all information must match exactly with data in the system";
            worksheet.Cells[14, 1].Value = "9. Students not found or with mismatched data will be rejected (unless auto-creation is enabled)";
            worksheet.Cells[15, 1].Value = "10. Only lecturers can import students into their own courses";

            // Auto-fit columns
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            // Freeze header row
            worksheet.View.FreezePanes(2, 1);

            return package.GetAsByteArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating course students template");
            throw;
        }
    }

    private static bool IsRowEmpty(ExcelWorksheet worksheet, int row, int columnCount)
    {
        for (int col = 1; col <= columnCount; col++)
        {
            if (!string.IsNullOrWhiteSpace(worksheet.Cells[row, col].Value?.ToString()))
            {
                return false;
            }
        }
        return true;
    }

    public byte[] ExportToExcel<T>(IEnumerable<T> data, string sheetName)
    {
        try
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add(sheetName);

            // Get properties
            var properties = typeof(T).GetProperties();
            
            // Add headers
            for (int i = 0; i < properties.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = properties[i].Name;
                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                worksheet.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
            }

            // Add data
            var dataList = data.ToList();
            for (int row = 0; row < dataList.Count; row++)
            {
                for (int col = 0; col < properties.Length; col++)
                {
                    var value = properties[col].GetValue(dataList[row]);
                    worksheet.Cells[row + 2, col + 1].Value = value;
                }
            }

            // Auto-fit columns
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            // Freeze header row
            worksheet.View.FreezePanes(2, 1);

            return package.GetAsByteArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting data to Excel");
            throw;
        }
    }
}