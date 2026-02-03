using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using PayOS.Models.Webhooks;
using UserService.Application.Common.Models;
using UserService.Application.Features.Payments.Commands;
using UserService.Application.Features.Payments.Queries;
using UserService.Domain.Enums;
using UserService.Infrastructure.Configuration;

namespace UserService.Application.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly PayOSSettings _payOsSettings;

    private const string BillingTestFallback = "http://127.0.0.1:3000/docs/billing-test.html";

    public PaymentsController(IMediator mediator, IOptions<PayOSSettings> payOsOptions)
    {
        _mediator = mediator;
        _payOsSettings = payOsOptions.Value;
    }

    [Authorize]
    [HttpPost("subscription")]
    public async Task<ActionResult<ResponseModel>> CreateSubscriptionPayment([FromBody] CreateSubscriptionPaymentRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Invalid user token" });
        }

        var command = new CreateSubscriptionPaymentCommand
        {
            UserId = userId,
            SubscriptionPlanId = request.SubscriptionPlanId,
            ReturnUrl = request.ReturnUrl,
            CancelUrl = request.CancelUrl
        };

        var response = await _mediator.Send(command);
        return StatusCode((int)(response.Status ?? HttpStatusCode.OK), response);
    }

    [Authorize]
    [HttpPost("subscription/confirm")]
    public async Task<ActionResult<ResponseModel>> ConfirmSubscriptionPayment([FromBody] ConfirmSubscriptionPaymentRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Invalid user token" });
        }

        var command = new ConfirmSubscriptionPaymentCommand
        {
            UserId = userId,
            OrderCode = request.OrderCode,
            Token = request.Token
        };

        var response = await _mediator.Send(command);
        return StatusCode((int)(response.Status ?? HttpStatusCode.OK), response);
    }

    [Authorize]
    [HttpPost("subscription/cancel")]
    public async Task<ActionResult<ResponseModel>> CancelSubscriptionPayment([FromBody] CancelSubscriptionPaymentRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Invalid user token" });
        }

        var command = new CancelSubscriptionPaymentCommand
        {
            UserId = userId,
            OrderCode = request.OrderCode,
            Reason = request.Reason
        };

        var response = await _mediator.Send(command);
        return StatusCode((int)(response.Status ?? HttpStatusCode.OK), response);
    }

    [Authorize]
    [HttpGet("subscription/history")]
    public async Task<ActionResult<ResponseModel>> GetSubscriptionPaymentHistory([FromQuery] PaymentHistoryFilter filter)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Invalid user token" });
        }

        var query = new GetSubscriptionPaymentsQuery
        {
            UserId = userId,
            IncludeAllUsers = false,
            TierId = filter.TierId,
            Status = filter.Status,
            From = filter.From,
            To = filter.To,
            Page = filter.Page,
            PageSize = filter.PageSize
        };

        var response = await _mediator.Send(query);
        return StatusCode((int)(response.Status ?? HttpStatusCode.OK), response);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet("subscription/admin")]
    public async Task<ActionResult<ResponseModel>> GetSubscriptionPaymentsForAdmin([FromQuery] AdminPaymentFilter filter)
    {
        var query = new GetSubscriptionPaymentsQuery
        {
            IncludeAllUsers = true,
            UserId = filter.UserId,
            TierId = filter.TierId,
            Status = filter.Status,
            From = filter.From,
            To = filter.To,
            Page = filter.Page,
            PageSize = filter.PageSize
        };

        var response = await _mediator.Send(query);
        return StatusCode((int)(response.Status ?? HttpStatusCode.OK), response);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet("subscription/admin/summary")]
    public async Task<ActionResult<ResponseModel>> GetSubscriptionPaymentsSummary([FromQuery] PaymentSummaryFilter filter)
    {
        var query = new GetSubscriptionPaymentsSummaryQuery
        {
            UserId = filter.UserId,
            TierId = filter.TierId,
            Status = filter.Status,
            From = filter.From,
            To = filter.To
        };

        var response = await _mediator.Send(query);
        return StatusCode((int)(response.Status ?? HttpStatusCode.OK), response);
    }

    [AllowAnonymous]
    [HttpPost("payos/webhook")]
    public async Task<ActionResult<ResponseModel>> HandlePayOSWebhook([FromBody] Webhook webhook)
    {
        if (webhook == null)
        {
            return BadRequest(new { message = "Webhook payload is required" });
        }

        var signature = Request.Headers["x-signature"].FirstOrDefault()
            ?? Request.Headers["X-Signature"].FirstOrDefault()
            ?? Request.Headers["payos-signature"].FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(signature) && string.IsNullOrWhiteSpace(webhook.Signature))
        {
            webhook.Signature = signature;
        }

        var command = new HandlePayOSWebhookCommand
        {
            Payload = webhook
        };

        var response = await _mediator.Send(command);
        var statusCode = (int)(response.Status ?? HttpStatusCode.OK);
        return StatusCode(statusCode, response);
    }

    [AllowAnonymous]
    [HttpGet("payos/return")]
    public async Task<IActionResult> HandlePayOsReturn([FromQuery] PayOsReturnPayload payload)
    {
        if (!string.IsNullOrWhiteSpace(payload.OrderCode) && !string.IsNullOrWhiteSpace(payload.ConfirmationToken))
        {
            var command = new ConfirmSubscriptionPaymentFromReturnCommand
            {
                OrderCode = payload.OrderCode,
                Token = payload.ConfirmationToken
            };

            try
            {
                await _mediator.Send(command, HttpContext.RequestAborted);
            }
            catch
            {
                // Continue redirect even if confirmation fails.
            }
        }

        var target = string.IsNullOrWhiteSpace(payload.TargetUrl)
            ? (string.IsNullOrWhiteSpace(_payOsSettings.ReturnUrl) ? BillingTestFallback : _payOsSettings.ReturnUrl)
            : payload.TargetUrl;

        var forwardParams = new Dictionary<string, string?>
        {
            ["status"] = payload.Status ?? payload.Code,
            ["code"] = payload.Code,
            ["orderCode"] = payload.OrderCode,
            ["amount"] = payload.Amount?.ToString(CultureInfo.InvariantCulture),
            ["message"] = payload.Message ?? payload.Desc,
            ["signature"] = payload.Signature,
            ["transactionId"] = payload.TransactionId,
            ["reference"] = payload.Reference,
            ["extraData"] = payload.ExtraData,
            ["confirmationToken"] = payload.ConfirmationToken
        };

        var redirectUrl = QueryHelpers.AddQueryString(
            target,
            forwardParams.Where(kv => !string.IsNullOrWhiteSpace(kv.Value)).ToDictionary(kv => kv.Key, kv => kv.Value));

        return Redirect(redirectUrl);
    }
}

public class CreateSubscriptionPaymentRequest
{
    [Required]
    public Guid SubscriptionPlanId { get; set; }
    public string? ReturnUrl { get; set; }
    public string? CancelUrl { get; set; }
}

public class ConfirmSubscriptionPaymentRequest
{
    public string OrderCode { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}

public class CancelSubscriptionPaymentRequest
{
    [Required]
    public string OrderCode { get; set; } = string.Empty;
    public string? Reason { get; set; }
}

public class PaymentHistoryFilter : PaymentFilterBase
{
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}

public class AdminPaymentFilter : PaymentHistoryFilter
{
    public Guid? UserId { get; set; }
}

public class PaymentSummaryFilter : PaymentFilterBase
{
    public Guid? UserId { get; set; }
}

public class PaymentFilterBase
{
    public Guid? TierId { get; set; }
    public SubscriptionPaymentStatus? Status { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

public record PayOsReturnPayload
{
    public string? Code { get; init; }
    public string? Status { get; init; }
    public string? Desc { get; init; }
    public string? Message { get; init; }
    public string? OrderCode { get; init; }
    public long? Amount { get; init; }
    public string? Signature { get; init; }
    public string? TransactionId { get; init; }
    public string? Reference { get; init; }
    public string? ExtraData { get; init; }
    public string? TargetUrl { get; init; }
    public string? ConfirmationToken { get; init; }
}
