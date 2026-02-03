using MediatR;
using UserService.Application.Common.Models;

namespace UserService.Application.Features.ApiKeys.Queries;

public class GetApiKeysQuery : IRequest<ResponseModel>
{
    public Guid UserId { get; set; }
}