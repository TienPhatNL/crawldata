using MediatR;

namespace UserService.Application.Features.Configuration.Commands;

/// <summary>
/// Command to toggle the active status of an allowed email domain (Admin only)
/// </summary>
public class ToggleEmailDomainStatusCommand : IRequest<ToggleEmailDomainStatusResponse>
{
    public Guid DomainId { get; set; }
    public Guid RequestedBy { get; set; }
}

public class ToggleEmailDomainStatusResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool NewStatus { get; set; }
}
