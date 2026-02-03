using ClassroomService.Application.Features.Dashboard.DTOs;
using ClassroomService.Domain.DTOs;
using MediatR;

namespace ClassroomService.Application.Features.Dashboard.Queries.GetUserTerms;

public record GetUserTermsQuery : IRequest<DashboardResponse<UserTermsDto>>;
