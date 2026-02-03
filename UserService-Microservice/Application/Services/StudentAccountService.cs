using System.Net;
using MediatR;
using UserService.Application.Features.Users.Commands;
using UserService.Domain.Interfaces;

namespace UserService.Application.Services;

/// <summary>
/// Implementation of IStudentAccountService that delegates to MediatR command handlers
/// This allows Infrastructure layer to call Application layer logic without direct coupling
/// </summary>
public class StudentAccountService : IStudentAccountService
{
    private readonly IMediator _mediator;

    public StudentAccountService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<Domain.Interfaces.StudentAccountCreationResult> CreateStudentAccountsAsync(
        Domain.Interfaces.CreateStudentAccountsRequest request,
        CancellationToken cancellationToken = default)
    {
        // Map from Domain request to Application command
        var command = new CreateStudentAccountsCommand
        {
            RequestedBy = request.RequestedBy,
            SendEmailCredentials = request.SendEmailCredentials,
            CreateAccountIfNotFound = request.CreateAccountIfNotFound,
            Students = request.Students.Select(s => new StudentAccountRequest
            {
                Email = s.Email,
                FirstName = s.FirstName,
                LastName = s.LastName,
                StudentId = s.StudentId,
                PhoneNumber = s.PhoneNumber
            }).ToList()
        };

        // Execute the command via MediatR
        var response = await _mediator.Send(command, cancellationToken);

        // Map from ResponseModel to Domain result
        var isSuccess = response.Status == HttpStatusCode.OK;
        
        // Extract data from ResponseModel's anonymous object
        var dataJson = System.Text.Json.JsonSerializer.Serialize(response.Data);
        var dataObj = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(dataJson);

        var totalRequested = dataObj.TryGetProperty("totalRequested", out var tr) ? tr.GetInt32() : 0;
        var successfullyCreated = dataObj.TryGetProperty("successfullyCreated", out var sc) ? sc.GetInt32() : 0;
        var failed = dataObj.TryGetProperty("failed", out var f) ? f.GetInt32() : 0;

        var results = new List<Domain.Interfaces.StudentAccountResult>();
        if (dataObj.TryGetProperty("results", out var resultsArray) && resultsArray.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (var item in resultsArray.EnumerateArray())
            {
                results.Add(new Domain.Interfaces.StudentAccountResult
                {
                    Email = item.TryGetProperty("email", out var email) ? email.GetString() ?? "" : "",
                    StudentId = item.TryGetProperty("studentId", out var sid) ? sid.GetString() ?? "" : "",
                    Success = item.TryGetProperty("success", out var success) && success.GetBoolean(),
                    ErrorMessage = item.TryGetProperty("errorMessage", out var err) ? err.GetString() : null,
                    UserId = item.TryGetProperty("userId", out var uid) && uid.ValueKind == System.Text.Json.JsonValueKind.String 
                        ? Guid.Parse(uid.GetString()!) : null,
                    TemporaryPassword = item.TryGetProperty("temporaryPassword", out var tp) ? tp.GetString() : null
                });
            }
        }

        return new Domain.Interfaces.StudentAccountCreationResult
        {
            IsSuccess = isSuccess,
            Message = response.Message ?? "",
            TotalRequested = totalRequested,
            SuccessfullyCreated = successfullyCreated,
            Failed = failed,
            Results = results
        };
    }
}
