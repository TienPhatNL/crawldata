using MediatR;
using UserService.Application.Common.Models;

namespace UserService.Application.Features.Subscriptions.Commands;

public class UpgradeSubscriptionCommand : IRequest<ResponseModel>
{
    public Guid UserId { get; set; }
    public Guid SubscriptionPlanId { get; set; }
    public int? CustomQuotaLimit { get; set; }
    public string? PaymentReference { get; set; }
    public bool IsRenewal { get; set; }
}