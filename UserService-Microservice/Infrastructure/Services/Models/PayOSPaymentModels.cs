using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UserService.Infrastructure.Services.Models;

public class PayOSPaymentLinkRequest
{
    [Range(1, long.MaxValue)]
    public long OrderCode { get; set; }

    [Range(1, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    public string Description { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
    public string? CancelUrl { get; set; }
    public string? BuyerName { get; set; }
    [EmailAddress]
    public string? BuyerEmail { get; set; }
    public string? BuyerPhone { get; set; }
    public string? BuyerCompanyName { get; set; }
    public string? BuyerAddress { get; set; }
    public DateTimeOffset? ExpiredAt { get; set; }
    public bool? BuyerNotGetInvoice { get; set; }
    public List<PayOSPaymentItem> Items { get; set; } = new();
}

public class PayOSPaymentItem
{
    [Required]
    public string Name { get; set; } = string.Empty;
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; } = 1;
    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }
    public string? Unit { get; set; }
}

public class PayOSPaymentLinkResponse
{
    public string PaymentLinkId { get; set; } = string.Empty;
    public string OrderCode { get; set; } = string.Empty;
    public string CheckoutUrl { get; set; } = string.Empty;
    public string? QrCode { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? ExpiredAt { get; set; }
    public string RawPayload { get; set; } = string.Empty;
}
