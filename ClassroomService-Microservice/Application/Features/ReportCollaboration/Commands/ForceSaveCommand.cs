using ClassroomService.Domain.DTOs;
using MediatR;

namespace ClassroomService.Application.Features.ReportCollaboration.Commands;

public class ForceSaveCommand : IRequest<ManualSaveResponse>
{
    public Guid ReportId { get; set; }
    public Guid UserId { get; set; }
    public string UserRole { get; set; } = string.Empty;
}
