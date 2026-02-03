using ClassroomService.Application.Features.Enrollments.Commands;
using ClassroomService.Domain.Constants;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;
using MediatR;

namespace ClassroomService.Application.Features.Enrollments.Commands;

public class ImportStudentEnrollmentsCommandHandler : IRequestHandler<ImportStudentEnrollmentsCommand, ImportStudentEnrollmentsResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKafkaUserService _userService;
    private readonly IExcelService _excelService;
    private readonly ILogger<ImportStudentEnrollmentsCommandHandler> _logger;

    public ImportStudentEnrollmentsCommandHandler(
        IUnitOfWork unitOfWork,
        IKafkaUserService userService,
        IExcelService excelService,
        ILogger<ImportStudentEnrollmentsCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _userService = userService;
        _excelService = excelService;
        _logger = logger;
    }

    public async Task<ImportStudentEnrollmentsResponse> Handle(ImportStudentEnrollmentsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate user authorization (lecturer or staff only)
            var user = await _userService.GetUserByIdAsync(request.ImportedBy, cancellationToken);
            if (user == null)
            {
                return new ImportStudentEnrollmentsResponse
                {
                    Success = false,
                    Message = "User not found",
                    TotalRows = 0,
                    SuccessfulEnrollments = 0,
                    FailedEnrollments = 0,
                    Errors = new List<string> { "User not found" },
                    EnrolledCourseIds = new List<Guid>()
                };
            }

            bool isStaff = user.Role == RoleConstants.Staff || user.Role == RoleConstants.Admin;
            bool isLecturer = user.Role == RoleConstants.Lecturer;

            if (!isStaff && !isLecturer)
            {
                return new ImportStudentEnrollmentsResponse
                {
                    Success = false,
                    Message = "You are not authorized to import student enrollments. Only lecturers and staff can perform this operation.",
                    TotalRows = 0,
                    SuccessfulEnrollments = 0,
                    FailedEnrollments = 0,
                    Errors = new List<string> { "Unauthorized access - Lecturers and Staff only" },
                    EnrolledCourseIds = new List<Guid>()
                };
            }

            // Validate file
            if (request.ExcelFile == null || request.ExcelFile.Length == 0)
            {
                return new ImportStudentEnrollmentsResponse
                {
                    Success = false,
                    Message = "Excel file is required",
                    TotalRows = 0,
                    SuccessfulEnrollments = 0,
                    FailedEnrollments = 0,
                    Errors = new List<string> { "No file provided" },
                    EnrolledCourseIds = new List<Guid>()
                };
            }

            // Check file extension
            var fileExtension = Path.GetExtension(request.ExcelFile.FileName).ToLowerInvariant();
            if (fileExtension != ".xlsx" && fileExtension != ".xls")
            {
                return new ImportStudentEnrollmentsResponse
                {
                    Success = false,
                    Message = "Invalid file format. Only .xlsx and .xls files are supported",
                    TotalRows = 0,
                    SuccessfulEnrollments = 0,
                    FailedEnrollments = 0,
                    Errors = new List<string> { "Invalid file format" },
                    EnrolledCourseIds = new List<Guid>()
                };
            }

            // Import data from Excel
            List<ImportStudentEnrollmentDto> importData;
            using (var stream = request.ExcelFile.OpenReadStream())
            {
                importData = await _excelService.ImportStudentEnrollmentsFromExcelAsync(stream);
            }

            if (!importData.Any())
            {
                return new ImportStudentEnrollmentsResponse
                {
                    Success = false,
                    Message = "No data found in Excel file",
                    TotalRows = 0,
                    SuccessfulEnrollments = 0,
                    FailedEnrollments = 0,
                    Errors = new List<string> { "No data rows found" },
                    EnrolledCourseIds = new List<Guid>()
                };
            }

            var errors = new List<string>();
            var enrolledCourseIds = new List<Guid>();
            var successfulEnrollments = 0;
            var failedEnrollments = 0;
            var createdStudentEmails = new List<string>();
            var failedRowNumbers = new HashSet<int>(); // Track rows that already failed

            // Get all course codes and terms in batch and convert to dictionaries
            var courseCodesList = await _unitOfWork.CourseCodes.GetActiveCourseCodesAsync(cancellationToken);
            var courseCodes = courseCodesList.ToDictionary(cc => cc.Code, cc => cc, StringComparer.OrdinalIgnoreCase);

            var termsList = await _unitOfWork.Terms.GetActiveTermsAsync(cancellationToken);
            var terms = termsList.ToDictionary(t => t.Name, t => t, StringComparer.OrdinalIgnoreCase);

            var studentEmails = importData.Select(d => d.StudentEmail).Distinct().ToList();
            _logger.LogInformation("Looking up {Count} unique student emails: {Emails}", 
                studentEmails.Count, string.Join(", ", studentEmails));
                
            var students = await _userService.GetUsersByEmailsAsync(studentEmails, cancellationToken);
            _logger.LogInformation("UserService returned {Count} users", students?.Count ?? 0);
            
            if (students != null && students.Any())
            {
                foreach (var student in students)
                {
                    _logger.LogInformation("Found student: {Email} (ID: {Id}, Role: {Role})", 
                        student.Email, student.Id, student.Role);
                }
            }
            else
            {
                _logger.LogWarning("No students found from UserService");
            }
            
            // Use case-insensitive dictionary to prevent KeyNotFoundException
            var studentDict = (students ?? new List<UserDto>()).Where(s => s.Role == RoleConstants.Student)
                                   .ToDictionary(s => s.Email, s => s, StringComparer.OrdinalIgnoreCase);
                                   
            _logger.LogInformation("Created student dictionary with {Count} students", studentDict.Count);

            // AUTO-CREATION LOGIC: Identify missing students and create them if enabled
            if (request.CreateAccountIfNotFound)
            {
                var missingStudents = importData
                    .Where(d => !studentDict.ContainsKey(d.StudentEmail))
                    .ToList();

                if (missingStudents.Any())
                {
                    _logger.LogInformation("Found {Count} missing students, attempting auto-creation", missingStudents.Count);

                    var studentsToCreate = new List<StudentAccountRequest>();

                    foreach (var missing in missingStudents)
                    {
                        // Validate required fields for creation
                        if (string.IsNullOrWhiteSpace(missing.FirstName) || 
                            string.IsNullOrWhiteSpace(missing.LastName) ||
                            string.IsNullOrWhiteSpace(missing.StudentId))
                        {
                            errors.Add($"Row {missing.RowNumber}: Missing required fields (First Name, Last Name, Student ID) for auto-creation of {missing.StudentEmail}");
                            failedEnrollments++;
                            failedRowNumbers.Add(missing.RowNumber); // Mark as failed
                            continue;
                        }

                        studentsToCreate.Add(new StudentAccountRequest
                        {
                            Email = missing.StudentEmail,
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

                            _logger.LogInformation("Auto-creation result: {Success}/{Total} students created",
                                createResponse.SuccessfullyCreated, createResponse.TotalRequested);

                            // Track successfully created students
                            var successfullyCreatedEmails = createResponse.Results
                                .Where(r => r.Success)
                                .Select(r => r.Email)
                                .ToList();

                            createdStudentEmails.AddRange(successfullyCreatedEmails);

                            // Track failed creations and mark them as failed enrollments
                            foreach (var failedResult in createResponse.Results.Where(r => !r.Success))
                            {
                                var importRow = importData.First(d => d.StudentEmail.Equals(failedResult.Email, StringComparison.OrdinalIgnoreCase));
                                errors.Add($"Row {importRow.RowNumber}: Failed to create student account - {failedResult.ErrorMessage}");
                                failedEnrollments++;
                                failedRowNumbers.Add(importRow.RowNumber); // Mark as failed
                            }

                            // Re-fetch students to include newly created ones
                            if (successfullyCreatedEmails.Any())
                            {
                                var updatedStudents = await _userService.GetUsersByEmailsAsync(studentEmails, cancellationToken);
                                studentDict = (updatedStudents ?? new List<UserDto>())
                                    .Where(s => s.Role == RoleConstants.Student)
                                    .ToDictionary(s => s.Email, s => s, StringComparer.OrdinalIgnoreCase);

                                _logger.LogInformation("Updated student dictionary with {Count} students after auto-creation", studentDict.Count);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error during student auto-creation");
                            
                            // Mark ALL missing students as failed since auto-creation completely failed
                            foreach (var missingStudent in missingStudents)
                            {
                                // Only add error if not already tracked
                                if (!failedRowNumbers.Contains(missingStudent.RowNumber))
                                {
                                    errors.Add($"Row {missingStudent.RowNumber}: Student {missingStudent.StudentEmail} not found and auto-creation failed - {ex.Message}");
                                    failedEnrollments++;
                                    failedRowNumbers.Add(missingStudent.RowNumber); // Mark as failed
                                }
                            }
                        }
                    }
                }
            }

            foreach (var importEnrollment in importData)
            {
                try
                {
                    // SKIP rows that already failed during auto-creation
                    if (failedRowNumbers.Contains(importEnrollment.RowNumber))
                    {
                        _logger.LogDebug("Skipping row {RowNumber} - already failed during auto-creation", importEnrollment.RowNumber);
                        continue;
                    }

                    // Validate row data (including term name)
                    var rowErrors = ValidateImportRow(importEnrollment, courseCodes, terms, studentDict, request.CreateAccountIfNotFound);
                    if (rowErrors.Any())
                    {
                        errors.AddRange(rowErrors.Select(e => $"Row {importEnrollment.RowNumber}: {e}"));
                        failedEnrollments++;
                        continue;
                    }

                    // Check if student exists in dictionary before accessing
                    if (!studentDict.ContainsKey(importEnrollment.StudentEmail))
                    {
                        errors.Add($"Row {importEnrollment.RowNumber}: Student {importEnrollment.StudentEmail} not found in system (auto-creation may have failed)");
                        failedEnrollments++;
                        continue;
                    }

                    var courseCode = courseCodes[importEnrollment.CourseCode];
                    var student = studentDict[importEnrollment.StudentEmail];
                    var term = terms[importEnrollment.Term];

                    // Find the course by name for precise matching (name includes unique code)
                    var course = await _unitOfWork.Courses
                        .GetAsync(c => c.Name == importEnrollment.CourseName
                            && c.CourseCodeId == courseCode.Id
                            && c.TermId == term.Id, cancellationToken);

                    if (course == null)
                    {
                        errors.Add($"Row {importEnrollment.RowNumber}: Course '{importEnrollment.CourseName}' not found in term '{importEnrollment.Term}'");
                        failedEnrollments++;
                        continue;
                    }

                    // Security check: Lecturers can only enroll students in their own courses
                    if (isLecturer && course.LecturerId != user.Id)
                    {
                        errors.Add($"Row {importEnrollment.RowNumber}: Lecturers can only enroll students in their own courses. Course '{importEnrollment.CourseName}' is not taught by you.");
                        failedEnrollments++;
                        continue;
                    }

                    // Check if student is already enrolled
                    var existingEnrollment = await _unitOfWork.CourseEnrollments
                        .GetEnrollmentAsync(course.Id, student.Id, cancellationToken);

                    if (existingEnrollment != null)
                    {
                        errors.Add($"Row {importEnrollment.RowNumber}: Student {importEnrollment.StudentEmail} is already enrolled in this course");
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

                    await _unitOfWork.CourseEnrollments.AddAsync(enrollment, cancellationToken);
                    enrolledCourseIds.Add(course.Id);
                    successfulEnrollments++;

                    _logger.LogInformation("Enrolled student {StudentEmail} in course {CourseId} ({CourseName}) from Excel import row {RowNumber}",
                        importEnrollment.StudentEmail, course.Id, course.Name, importEnrollment.RowNumber);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing row {RowNumber} during enrollment import", importEnrollment.RowNumber);
                    errors.Add($"Row {importEnrollment.RowNumber}: Unexpected error - {ex.Message}");
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

            return new ImportStudentEnrollmentsResponse
            {
                Success = successfulEnrollments > 0,
                Message = message,
                TotalRows = importData.Count,
                SuccessfulEnrollments = successfulEnrollments,
                FailedEnrollments = failedEnrollments,
                StudentsCreated = createdStudentEmails.Count,
                Errors = errors,
                EnrolledCourseIds = enrolledCourseIds.Distinct().ToList(),
                CreatedStudentEmails = createdStudentEmails
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during student enrollment import");
            return new ImportStudentEnrollmentsResponse
            {
                Success = false,
                Message = $"Import failed: {ex.Message}",
                TotalRows = 0,
                SuccessfulEnrollments = 0,
                FailedEnrollments = 0,
                StudentsCreated = 0,
                Errors = new List<string> { ex.Message },
                EnrolledCourseIds = new List<Guid>(),
                CreatedStudentEmails = new List<string>()
            };
        }
    }

    private static List<string> ValidateImportRow(ImportStudentEnrollmentDto importEnrollment, 
        Dictionary<string, CourseCode> courseCodes, Dictionary<string, Term> terms, Dictionary<string, Domain.DTOs.UserDto> students, bool createAccountIfNotFound)
    {
        var errors = new List<string>();

        // Validate student email
        if (string.IsNullOrWhiteSpace(importEnrollment.StudentEmail))
        {
            errors.Add("Student Email is required");
        }
        else if (!students.ContainsKey(importEnrollment.StudentEmail))
        {
            // Student not found - handle based on auto-creation setting
            if (!createAccountIfNotFound)
            {
                errors.Add($"Student with email '{importEnrollment.StudentEmail}' not found in system. Enable auto-creation to create missing students.");
            }
        }
        else
        {
            // Student exists - validate all data matches
            var student = students[importEnrollment.StudentEmail];
            
            // Validate Student ID matches the email
            if (string.IsNullOrWhiteSpace(importEnrollment.StudentId))
            {
                errors.Add("Student ID is required");
            }
            else if (string.IsNullOrWhiteSpace(student.StudentId))
            {
                errors.Add($"Student '{importEnrollment.StudentEmail}' does not have a Student ID in the system");
            }
            else if (!importEnrollment.StudentId.Equals(student.StudentId, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"Student ID '{importEnrollment.StudentId}' does not match the Student ID '{student.StudentId}' associated with email '{importEnrollment.StudentEmail}'");
            }

            // Validate First Name matches
            if (!string.IsNullOrWhiteSpace(importEnrollment.FirstName))
            {
                if (!importEnrollment.FirstName.Equals(student.FirstName, StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add($"First Name '{importEnrollment.FirstName}' does not match '{student.FirstName}' for email '{importEnrollment.StudentEmail}'");
                }
            }

            // Validate Last Name matches
            if (!string.IsNullOrWhiteSpace(importEnrollment.LastName))
            {
                if (!importEnrollment.LastName.Equals(student.LastName, StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add($"Last Name '{importEnrollment.LastName}' does not match '{student.LastName}' for email '{importEnrollment.StudentEmail}'");
                }
            }

            // Validate Profile Picture URL matches (if provided in Excel)
            if (!string.IsNullOrWhiteSpace(importEnrollment.ProfilePictureUrl))
            {
                if (string.IsNullOrWhiteSpace(student.ProfilePictureUrl))
                {
                    errors.Add($"Student '{importEnrollment.StudentEmail}' does not have a Profile Picture URL in the system, but one was provided in Excel");
                }
                else if (!importEnrollment.ProfilePictureUrl.Equals(student.ProfilePictureUrl, StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add($"Profile Picture URL mismatch for '{importEnrollment.StudentEmail}'");
                }
            }
        }

        // Validate course code
        if (string.IsNullOrWhiteSpace(importEnrollment.CourseCode))
        {
            errors.Add("Course Code is required");
        }
        else if (!courseCodes.ContainsKey(importEnrollment.CourseCode))
        {
            errors.Add($"Course Code '{importEnrollment.CourseCode}' not found or inactive");
        }

        // Validate course name (required - contains the unique code)
        if (string.IsNullOrWhiteSpace(importEnrollment.CourseName))
        {
            errors.Add("Course Name is required");
        }
        else if (importEnrollment.CourseName.Length < 5 || importEnrollment.CourseName.Length > 200)
        {
            errors.Add("Course Name must be between 5 and 200 characters");
        }

        // Validate term name
        if (string.IsNullOrWhiteSpace(importEnrollment.Term))
        {
            errors.Add("Term Name is required");
        }
        else if (!terms.ContainsKey(importEnrollment.Term))
        {
            errors.Add($"Term '{importEnrollment.Term}' not found or inactive");
        }

        return errors;
    }
}