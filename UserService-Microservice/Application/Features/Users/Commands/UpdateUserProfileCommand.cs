using MediatR;
using UserService.Application.Common.Models;

namespace UserService.Application.Features.Users.Commands;

public class UpdateUserProfileCommand : IRequest<ResponseModel>
{
    public Guid UserId { get; private set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? InstitutionName { get; set; }
    public string? InstitutionAddress { get; set; }
    public string? StudentId { get; set; }
    public string? Department { get; set; }

    public UpdateUserProfileCommand(Guid userId)
    {
        UserId = userId;
    }

    // Parameterless constructor for model binding - UserId should be set from route/context
    public UpdateUserProfileCommand()
    {
    }

    public void SetUserId(Guid userId)
    {
        UserId = userId;
    }
}