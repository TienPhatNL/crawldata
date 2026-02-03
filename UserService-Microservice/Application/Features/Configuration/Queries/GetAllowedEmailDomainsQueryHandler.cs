using MediatR;
using System.Linq;
using UserService.Infrastructure.Repositories;

namespace UserService.Application.Features.Configuration.Queries;

public class GetAllowedEmailDomainsQueryHandler : IRequestHandler<GetAllowedEmailDomainsQuery, List<AllowedEmailDomainDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAllowedEmailDomainsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<AllowedEmailDomainDto>> Handle(GetAllowedEmailDomainsQuery request, CancellationToken cancellationToken)
    {
        IEnumerable<UserService.Domain.Entities.AllowedEmailDomain> domains;
        
        if (request.OnlyActive)
        {
            domains = await _unitOfWork.AllowedEmailDomains.GetManyAsync(d => d.IsActive, cancellationToken);
        }
        else
        {
            domains = await _unitOfWork.AllowedEmailDomains.GetAllAsync(cancellationToken);
        }

        return domains.OrderBy(d => d.Domain)
            .Select(d => new AllowedEmailDomainDto
            {
                Id = d.Id,
                Domain = d.Domain,
                Description = d.Description,
                IsActive = d.IsActive,
                AllowSubdomains = d.AllowSubdomains,
                CreatedAt = d.CreatedAt
            })
            .ToList();
    }
}
