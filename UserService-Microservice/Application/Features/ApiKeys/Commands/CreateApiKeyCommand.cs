using MediatR;
using UserService.Application.Common.Models;

namespace UserService.Application.Features.ApiKeys.Commands;

public class CreateApiKeyCommand : IRequest<ResponseModel>
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public List<string>? Scopes { get; set; }
}