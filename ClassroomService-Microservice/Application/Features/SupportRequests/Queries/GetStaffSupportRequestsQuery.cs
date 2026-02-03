using MediatR;
using ClassroomService.Domain.Common;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Application.Features.SupportRequests.Queries;

public class GetStaffSupportRequestsQuery : IRequest<GetStaffSupportRequestsResponse>
{
    public Guid StaffId { get; set; }
    public string? Status { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class GetStaffSupportRequestsResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public PagedResult<SupportRequestDto>? Data { get; set; }
}
