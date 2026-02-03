using MediatR;
using ClassroomService.Domain.Common;
using ClassroomService.Domain.DTOs;

namespace ClassroomService.Application.Features.SupportRequests.Queries;

public class GetPendingSupportRequestsQuery : IRequest<GetPendingSupportRequestsResponse>
{
    public Guid? CourseId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class GetPendingSupportRequestsResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public PagedResult<SupportRequestListDto>? Data { get; set; }
}
