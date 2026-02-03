using MediatR;
using UserService.Application.Common.Models;

namespace UserService.Application.Features.ApiKeys.Commands;

public class RevokeApiKeyCommand : IRequest<ResponseModel>
{
    public Guid UserId { get; set; }
    public Guid ApiKeyId { get; set; }
}