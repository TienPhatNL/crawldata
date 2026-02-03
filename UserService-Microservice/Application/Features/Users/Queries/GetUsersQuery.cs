using MediatR;
using UserService.Application.Common.Models;
using UserService.Domain.Enums;

namespace UserService.Application.Features.Users.Queries;

public class GetUsersQuery : IRequest<ResponseModel>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SearchTerm { get; set; }
    public UserRole? Role { get; set; }
    public UserStatus? Status { get; set; }
    public string? SubscriptionTierName { get; set; } // Changed to string for tier name filtering
    public string? SortBy { get; set; } = "CreatedAt";
    public string? SortOrder { get; set; } = "desc";
    public List<string>? Emails { get; set; } // Added for batch email lookup (PNLT feature)
}