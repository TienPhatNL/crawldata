using MediatR;

namespace UserService.Application.Features.Configuration.Queries;

public class GetAllowedEmailDomainsQuery : IRequest<List<AllowedEmailDomainDto>>
{
    public bool OnlyActive { get; set; } = true;
}

public class AllowedEmailDomainDto
{
    public Guid Id { get; set; }
    public string Domain { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool AllowSubdomains { get; set; }
    public DateTime CreatedAt { get; set; }
}
