using ClassroomService.Domain.Constants;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;
using MediatR;

namespace ClassroomService.Application.Features.Enrollments.Commands;

public class ImportCourseStudentsCommandHandler : IRequestHandler<ImportCourseStudentsCommand, ImportCourseStudentsResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKafkaUserService _userService;
    private readonly IExcelService _excelService;
    private readonly ILogger<ImportCourseStudentsCommandHandler> _logger;

    public ImportCourseStudentsCommandHandler(
        IUnitOfWork unitOfWork,
        IKafkaUserService userService,
        IExcelService excelService,
        ILogger<ImportCourseStudentsCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _userService = userService;
        _excelService = excelService;
        _logger = logger;
    }

    public async Task<ImportCourseStudentsResponse> Handle(ImportCourseStudentsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate that the course exists
            var course = await _unitOfWork.Courses.GetCourseWithDetailsAsync(request.CourseId, cancellationToken);

            if (course == null)
            {
                return new ImportCourseStudentsResponse
                {
                    Success = false,
                    Message = "Course not found",
                    TotalRows = 0,
                    SuccessfulEnrollments = 0,
                    FailedEnrollments = 0,
                    Errors = new List<string> { "Course not found" },
                    CourseId = request.CourseId,
                    CourseName = ""
                };
            }

            // Validate user authorization (lecturer only and must be the course lecturer)
            var user = await _userService.GetUserByIdAsync(request.ImportedBy, cancellationToken);
            if (user == null)
            {
                return new ImportCourseStudentsResponse
                {
                    Success = false,
                    Message = "User not found",
                    TotalRows = 0,
                    SuccessfulEnrollments = 0,
                    FailedEnrollments = 0,
                    Errors = new List<string> { "User not found" },
                    CourseId = request.CourseId,
                    CourseName = course.Name
                };
            }

            // Only lecturers can import and only into their own courses
            if (user.Role != RoleConstants.Lecturer)
            {
                return new ImportCourseStudentsResponse
                {
                    Success = false,
                    Message = "Only lecturers can import students into courses",
                    TotalRows = 0,
                    SuccessfulEnrollments = 0,
                    FailedEnrollments = 0,
                    Errors = new List<string> { "Unauthorized access - Lecturers only" },
                    CourseId = request.CourseId,
                    CourseName = course.Name
                };
            }

            if (course.LecturerId != user.Id)
            {
                return new ImportCourseStudentsResponse
                {
                    Success = false,
                    Message = "You can only import students into your own courses",
                    TotalRows = 0,
                    SuccessfulEnrollments = 0,
                    FailedEnrollments = 0,
                    Errors = new List<string> { "Unauthorized - You can only import students into your own courses" },
                    CourseId = request.CourseId,
                    CourseName = course.Name
                };
            }

            // Import data from Excel
            List<ImportCourseStudentsDto> importData;
            using (var stream = request.ExcelFile.OpenReadStream())
            {
                importData = await _excelService.ImportCourseStudentsFromExcelAsync(stream);
            }

            if (!importData.Any())
            {
                return new ImportCourseStudentsResponse
                {
                    Success = false,
                    Message = "No data found in Excel file",
                    TotalRows = 0,
                    SuccessfulEnrollments = 0,
                    FailedEnrollments = 0,
                    Errors = new List<string> { "No data rows found" },
                    CourseId = request.CourseId,
                    CourseName = course.Name
                };
            }

            var errors = new List<string>();
            var successfulEnrollments = 0;
            var failedEnrollments = 0;
            var createdStudentEmails = new List<string>();
            var failedRowNumbers = new HashSet<int>(); // Track rows that already failed

            // Get students from UserService
            var studentEmails = importData.Select(d => d.Email).Distinct().ToList();
            _logger.LogInformation("Looking up {Count} unique student emails for course {CourseId}: {Emails}",
                studentEmails.Count, request.CourseId, string.Join(", ", studentEmails));

            var students = await _userService.GetUsersByEmailsAsync(studentEmails, cancellationToken);
            _logger.LogInformation("UserService returned {Count} users for course {CourseId}", students?.Count ?? 0, request.CourseId);

            // FIX: Use case-insensitive dictionary to prevent KeyNotFoundException
            var studentDict = (students ?? new List<UserDto>()).Where(s => s.Role == RoleConstants.Student)
                                   .ToDictionary(s => s.Email, s => s, StringComparer.OrdinalIgnoreCase);

            _logger.LogInformation("Created student dictionary with {Count} students for course {CourseId}", studentDict.Count, request.CourseId);

            // AUTO-CREATION LOGIC: Identify missing students and create them if enabled
            if (request.CreateAccountIfNotFound)
            {
                var missingStudents = importData
                    .Where(d => !studentDict.ContainsKey(d.Email))
                    .ToList();

                if (missingStudents.Any())
                {
                    _logger.LogInformation("Found {Count} missing students for course {CourseId}, attempting auto-creation", 
                        missingStudents.Count, request.CourseId);

                    var studentsToCreate = new List<StudentAccountRequest>();

                    foreach (var missing in missingStudents)
                    {
                        // Validate required fields for creation
                        if (string.IsNullOrWhiteSpace(missing.FirstName) || 
                            string.IsNullOrWhiteSpace(missing.LastName) ||
                            string.IsNullOrWhiteSpace(missing.StudentId))
                        {
                            errors.Add($"Row {missing.RowNumber}: Missing required fields (First Name, Last Name, Student ID) for auto-creation of {missing.Email}");
                            failedEnrollments++;
                            failedRowNumbers.Add(missing.RowNumber); // Mark as failed
                            continue;
                        }

                        studentsToCreate.Add(new StudentAccountRequest
                        {
                            Email = missing.Email,
                            FirstName = missing.FirstName,
                            LastName = missing.LastName,
                            StudentId = missing.StudentId
                        });
                    }

                    if (studentsToCreate.Any())
                    {
                        try
                        {
                            var createRequest = new CreateStudentAccountsRequest
                            {
                                RequestedBy = request.ImportedBy,
                                Students = studentsToCreate,
                                SendEmailCredentials = true,
                                CreateAccountIfNotFound = true
                            };

                            var createResponse = await _userService.CreateStudentAccountsAsync(createRequest, cancellationToken);

                            _logger.LogInformation("Auto-creation result for course {CourseId}: {Success}/{Total} students created",
                                request.CourseId, createResponse.SuccessfullyCreated, createResponse.TotalRequested);

                            // Track successfully created students
                            var successfullyCreatedEmails = createResponse.Results
                                .Where(r => r.Success)
                                .Select(r => r.Email)
                                .ToList();

                            createdStudentEmails.AddRange(successfullyCreatedEmails);

                            // Track failed creations and mark them as failed enrollments
                            foreach (var failedResult in createResponse.Results.Where(r => !r.Success))
                            {
                                var importRow = importData.First(d => d.Email.Equals(failedResult.Email, StringComparison.OrdinalIgnoreCase));
                                errors.Add($"Row {importRow.RowNumber}: Failed to create student account - {failedResult.ErrorMessage}");
                                failedEnrollments++;
                                failedRowNumbers.Add(importRow.RowNumber); // Mark as failed
                            }

                            // Re-fetch students to include newly created ones
                            if (successfullyCreatedEmails.Any())
                            {
                                students = await _userService.GetUsersByEmailsAsync(studentEmails, cancellationToken);
                                // FIX: Use case-insensitive dictionary to prevent KeyNotFoundException
                                studentDict = (students ?? new List<UserDto>())
                                    .Where(s => s.Role == RoleConstants.Student)
                                    .ToDictionary(s => s.Email, s => s, StringComparer.OrdinalIgnoreCase);

                                _logger.LogInformation("Updated student dictionary with {Count} students after auto-creation for course {CourseId}", 
                                    studentDict.Count, request.CourseId);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error during student auto-creation for course {CourseId}", request.CourseId);
                            
                            // Mark ALL missing students as failed since auto-creation completely failed
                            foreach (var missingStudent in missingStudents)
                            {
                                // Only add error if not already tracked
                                if (!failedRowNumbers.Contains(missingStudent.RowNumber))
                                {
                                    errors.Add($"Row {missingStudent.RowNumber}: Student {missingStudent.Email} not found and auto-creation failed - {ex.Message}");
                                    failedEnrollments++;
                                    failedRowNumbers.Add(missingStudent.RowNumber); // Mark as failed
                                }
                            }
                        }
                    }
                }
            }

            foreach (var importStudent in importData)
            {
                try
                {
                    // SKIP rows that already failed during auto-creation
                    if (failedRowNumbers.Contains(importStudent.RowNumber))
                    {
                        _logger.LogDebug("Skipping row {RowNumber} - already failed during auto-creation", importStudent.RowNumber);
                        continue;
                    }

                    // Validate row data
                    var rowErrors = ValidateImportRow(importStudent, studentDict, request.CreateAccountIfNotFound);
                    if (rowErrors.Any())
                    {
                        errors.AddRange(rowErrors.Select(e => $"Row {importStudent.RowNumber}: {e}"));
                        failedEnrollments++;
                        continue;
                    }

                    // CRITICAL FIX: Check if student exists in dictionary before accessing
                    // This handles the case where auto-creation failed
                    if (!studentDict.ContainsKey(importStudent.Email))
                    {
                        errors.Add($"Row {importStudent.RowNumber}: Student {importStudent.Email} not found in system (auto-creation may have failed)");
                        failedEnrollments++;
                        continue;
                    }

                    var student = studentDict[importStudent.Email];

                    // Check if student is already enrolled
                    var existingEnrollment = await _unitOfWork.CourseEnrollments
                        .GetAsync(ce => ce.CourseId == course.Id
                            && ce.StudentId == student.Id, cancellationToken);

                    if (existingEnrollment != null)
                    {
                        errors.Add($"Row {importStudent.RowNumber}: Student {importStudent.Email} is already enrolled in this course");
                        failedEnrollments++;
                        continue;
                    }

                    // Create enrollment
                    var enrollment = new CourseEnrollment
                    {
                        Id = Guid.NewGuid(),
                        CourseId = course.Id,
                        StudentId = student.Id,
                        JoinedAt = DateTime.UtcNow,
                        Status = EnrollmentStatus.Active
                    };

                    await _unitOfWork.CourseEnrollments.AddAsync(enrollment);
                    successfulEnrollments++;

                    _logger.LogInformation("Enrolled student {StudentEmail} in course {CourseId} ({CourseName}) from Excel import row {RowNumber}",
                        importStudent.Email, course.Id, course.Name, importStudent.RowNumber);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing row {RowNumber} during course student import", importStudent.RowNumber);
                    errors.Add($"Row {importStudent.RowNumber}: Unexpected error - {ex.Message}");
                    failedEnrollments++;
                }
            }

            // Save all changes
            if (successfulEnrollments > 0)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            var message = successfulEnrollments > 0
                ? $"Import completed: {successfulEnrollments} successful enrollments, {failedEnrollments} failed"
                : "Import failed: No enrollments were successfully processed";

            return new ImportCourseStudentsResponse
            {
                Success = successfulEnrollments > 0,
                Message = message,
                TotalRows = importData.Count,
                SuccessfulEnrollments = successfulEnrollments,
                FailedEnrollments = failedEnrollments,
                StudentsCreated = createdStudentEmails.Count,
                Errors = errors,
                CreatedStudentEmails = createdStudentEmails,
                CourseId = request.CourseId,
                CourseName = course.Name
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during course student import for course {CourseId}", request.CourseId);
            return new ImportCourseStudentsResponse
            {
                Success = false,
                Message = $"Import failed: {ex.Message}",
                TotalRows = 0,
                SuccessfulEnrollments = 0,
                FailedEnrollments = 0,
                StudentsCreated = 0,
                Errors = new List<string> { ex.Message },
                CreatedStudentEmails = new List<string>(),
                CourseId = request.CourseId,
                CourseName = ""
            };
        }
    }

    private static List<string> ValidateImportRow(ImportCourseStudentsDto importStudent, Dictionary<string, UserDto> students, bool createAccountIfNotFound)
    {
        var errors = new List<string>();

        // Validate email
        if (string.IsNullOrWhiteSpace(importStudent.Email))
        {
            errors.Add("Email is required");
        }
        else if (!students.ContainsKey(importStudent.Email))
        {
            // Student not found
            if (!createAccountIfNotFound)
            {
                errors.Add($"Student with email '{importStudent.Email}' not found in system. Enable auto-creation to create missing students.");
            }
            // If auto-creation is enabled, validation of required fields is done earlier in the handler
        }
        else
        {
            // Student exists - validate all data matches
            var student = students[importStudent.Email];

            // Validate Student ID matches
            if (string.IsNullOrWhiteSpace(importStudent.StudentId))
            {
                errors.Add("Student ID is required");
            }
            else if (string.IsNullOrWhiteSpace(student.StudentId))
            {
                errors.Add($"Student '{importStudent.Email}' does not have a Student ID in the system");
            }
            else if (!importStudent.StudentId.Equals(student.StudentId, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"Student ID '{importStudent.StudentId}' does not match the Student ID '{student.StudentId}' for email '{importStudent.Email}'");
            }

            // Validate First Name matches
            if (string.IsNullOrWhiteSpace(importStudent.FirstName))
            {
                errors.Add("First Name is required");
            }
            else if (!importStudent.FirstName.Equals(student.FirstName, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"First Name '{importStudent.FirstName}' does not match the First Name '{student.FirstName}' for email '{$"{importStudent.Email}"}'");
            }

            // Validate Last Name matches
            if (string.IsNullOrWhiteSpace(importStudent.LastName))
            {
                errors.Add("Last Name is required");
            }
            else if (!importStudent.LastName.Equals(student.LastName, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"Last Name '{importStudent.LastName}' does not match the Last Name '{student.LastName}' for email '{$"{importStudent.Email}"}'");
            }

            // Validate Profile Picture URL matches (if provided)
            if (!string.IsNullOrWhiteSpace(importStudent.ProfilePictureUrl))
            {
                if (string.IsNullOrWhiteSpace(student.ProfilePictureUrl))
                {
                    errors.Add($"Student '{importStudent.Email}' does not have a Profile Picture URL in the system, but one was provided in Excel");
                }
                else if (!importStudent.ProfilePictureUrl.Equals(student.ProfilePictureUrl, StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add($"Profile Picture URL '{importStudent.ProfilePictureUrl}' does not match the Profile Picture URL '{student.ProfilePictureUrl}' for email '{$"{importStudent.Email}"}'");
                }
            }
        }

        return errors;
    }
}