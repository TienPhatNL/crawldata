using System.Net;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Application.Common.Models;
using UserService.Domain.Entities;
using UserService.Domain.Enums;
using UserService.Domain.Events;
using UserService.Infrastructure.Repositories;
using UserService.Infrastructure.Services;

namespace UserService.Application.Features.Users.Commands;

public class CreateStudentAccountsCommandHandler : IRequestHandler<CreateStudentAccountsCommand, ResponseModel>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly IEmailService _emailService;
    private readonly ILogger<CreateStudentAccountsCommandHandler> _logger;

    public CreateStudentAccountsCommandHandler(
        IUnitOfWork unitOfWork,
        IPasswordHashingService passwordHashingService,
        IEmailService emailService,
        ILogger<CreateStudentAccountsCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _passwordHashingService = passwordHashingService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<ResponseModel> Handle(CreateStudentAccountsCommand request, CancellationToken cancellationToken)
    {
        // Verify the requesting user has permission (Lecturer, Staff, or Admin)
        var requestingUser = await _unitOfWork.Users.GetByIdAsync(request.RequestedBy, cancellationToken);
        if (requestingUser?.Role != UserRole.Lecturer && 
            requestingUser?.Role != UserRole.Staff && 
            requestingUser?.Role != UserRole.Admin)
        {
            throw new ValidationException("Only Lecturers, Staff, and Admins can create student accounts");
        }

        var results = new List<object>();
        var successCount = 0;

        foreach (var studentRequest in request.Students)
        {
            try
            {
                // Check if email already exists
                var emailExists = await _unitOfWork.Users.EmailExistsAsync(studentRequest.Email, cancellationToken);
                
                if (emailExists)
                {
                    results.Add(new
                    {
                        email = studentRequest.Email,
                        studentId = studentRequest.StudentId,
                        success = false,
                        errorMessage = "Email already exists"
                    });
                    continue;
                }

                // If CreateAccountIfNotFound is false, and email doesn't exist, skip
                if (!request.CreateAccountIfNotFound && !emailExists)
                {
                    results.Add(new
                    {
                        email = studentRequest.Email,
                        studentId = studentRequest.StudentId,
                        success = false,
                        errorMessage = "Student account not found. Enable auto-creation to create account."
                    });
                    continue;
                }

                // Validate email domain if CreateAccountIfNotFound is true
                if (request.CreateAccountIfNotFound)
                {
                    var isAllowed = await IsEmailDomainAllowedAsync(studentRequest.Email, cancellationToken);
                    if (!isAllowed)
                    {
                        results.Add(new
                        {
                            email = studentRequest.Email,
                            studentId = studentRequest.StudentId,
                            success = false,
                            errorMessage = "Email domain not allowed for auto-creation. Contact administrator to add domain."
                        });
                        continue;
                    }
                }

                // Generate temporary password
                var temporaryPassword = GenerateTemporaryPassword();
                var passwordHash = _passwordHashingService.HashPassword(temporaryPassword);

                // Create student user
                var student = new User
                {
                    Id = Guid.NewGuid(),
                    Email = studentRequest.Email.ToLowerInvariant(),
                    PasswordHash = passwordHash,
                    FirstName = studentRequest.FirstName,
                    LastName = studentRequest.LastName,
                    PhoneNumber = studentRequest.PhoneNumber,
                    StudentId = studentRequest.StudentId,
                    Role = UserRole.Student,
                    Status = UserStatus.Active, // Students don't require approval when created by Staff/Lecturer
                    CurrentSubscriptionPlanId = Guid.Parse("11111111-1111-1111-1111-111111111111"), // Free plan
                    CrawlQuotaLimit = 4, // 4 URLs per assignment
                    QuotaResetDate = GetNextQuotaResetDate(),
                    RequiresApproval = false,
                    
                    // Auto-confirm email for Staff-created accounts
                    EmailConfirmedAt = DateTime.UtcNow,
                    
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = request.RequestedBy
                };

                // Add domain event
                student.AddDomainEvent(new UserRegisteredEvent(student.Id, student.Email, student.Role, false));

                // Save student
                await _unitOfWork.Users.AddAsync(student, cancellationToken);

                // Send email with credentials if requested
                if (request.SendEmailCredentials)
                {
                    try
                    {
                        await _emailService.SendStudentAccountCreatedEmailAsync(
                            student.Email,
                            student.FirstName,
                            student.StudentId!,
                            temporaryPassword,
                            cancellationToken);
                        
                        _logger.LogInformation("Credentials email sent to {Email}", student.Email);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send credentials email to {Email}, but account was created", student.Email);
                        // Continue - account creation succeeded even if email failed
                    }
                }

                results.Add(new
                {
                    email = studentRequest.Email,
                    studentId = studentRequest.StudentId,
                    success = true,
                    userId = student.Id,
                    temporaryPassword = request.SendEmailCredentials ? temporaryPassword : null
                });

                successCount++;
                
                _logger.LogInformation("Student account created: {UserId} ({Email}) by {RequestedBy}", 
                    student.Id, student.Email, request.RequestedBy);
            }
            catch (Exception ex)
            {
                results.Add(new
                {
                    email = studentRequest.Email,
                    studentId = studentRequest.StudentId,
                    success = false,
                    errorMessage = ex.Message
                });

                _logger.LogError(ex, "Failed to create student account for {Email}", studentRequest.Email);
            }
        }

        // Save all changes at the end
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Bulk student creation completed: {SuccessCount}/{TotalCount} successful by {RequestedBy}",
            successCount, request.Students.Count, request.RequestedBy);

        var message = $"Created {successCount} out of {request.Students.Count} student accounts";
        var data = new
        {
            totalRequested = request.Students.Count,
            successfullyCreated = successCount,
            failed = request.Students.Count - successCount,
            results
        };

        return new ResponseModel(HttpStatusCode.OK, message, data);
    }

    private async Task<bool> IsEmailDomainAllowedAsync(string email, CancellationToken cancellationToken)
    {
        // Extract domain from email
        var atIndex = email.IndexOf('@');
        if (atIndex < 0)
            return false;

        var domain = email.Substring(atIndex); // Includes @

        // Get all active allowed domains
        var allowedDomains = await _unitOfWork.AllowedEmailDomains
            .GetManyAsync(d => d.IsActive, cancellationToken);

        if (allowedDomains == null || !allowedDomains.Any())
        {
            _logger.LogWarning("No allowed email domains configured for auto-creation");
            return false;
        }

        // Check if domain matches any allowed domain
        foreach (var allowedDomain in allowedDomains)
        {
            if (allowedDomain.AllowSubdomains)
            {
                // Match subdomains: .edu matches @university.edu, @cs.university.edu, etc.
                if (domain.EndsWith(allowedDomain.Domain, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug("Email domain {Domain} allowed by subdomain rule {AllowedDomain}", 
                        domain, allowedDomain.Domain);
                    return true;
                }
            }
            else
            {
                // Exact match only
                if (domain.Equals(allowedDomain.Domain, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug("Email domain {Domain} allowed by exact match {AllowedDomain}", 
                        domain, allowedDomain.Domain);
                    return true;
                }
            }
        }

        _logger.LogWarning("Email domain {Domain} not allowed for auto-creation", domain);
        return false;
    }

    private static string GenerateTemporaryPassword()
    {
        // Generate a secure temporary password
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789@#$%";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 12)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private static DateTime GetNextQuotaResetDate()
    {
        // Students get quota reset at the beginning of each month
        return new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(1);
    }
}