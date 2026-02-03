using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.Common.Models;
using UserService.Application.Features.Admin.Dashboard.Queries;
using UserService.Application.Features.Admin.Dashboard.DTOs;

namespace UserService.Application.Controllers;

/// <summary>
/// Admin dashboard endpoints for viewing revenue analytics, payment statistics,
/// subscription metrics, and user insights.
/// </summary>
[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Policy = "AdminOnly")]
public class AdminDashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminDashboardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get revenue statistics including total revenue, revenue by tier, growth analysis, and timeline data.
    /// </summary>
    /// <param name="startDate">Start date for the query period (defaults to 30 days ago)</param>
    /// <param name="endDate">End date for the query period (defaults to today)</param>
    /// <param name="interval">Grouping interval: day, week, or month (defaults to day)</param>
    /// <returns>Revenue statistics with chart-ready data</returns>
    [HttpGet("revenue")]
    [ProducesResponseType(typeof(ResponseModel), 200)]
    public async Task<IActionResult> GetRevenueStatistics(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? interval)
    {
        var query = new GetRevenueStatisticsQuery
        {
            StartDate = startDate,
            EndDate = endDate,
            Interval = interval ?? "day"
        };

        var result = await _mediator.Send(query);
        return StatusCode((int)result.Status!, result);
    }

    /// <summary>
    /// Get payment statistics including order counts, status distribution, success rates, and failed payment analysis.
    /// </summary>
    /// <param name="startDate">Start date for the query period (defaults to 30 days ago)</param>
    /// <param name="endDate">End date for the query period (defaults to today)</param>
    /// <param name="interval">Grouping interval: day, week, or month (defaults to day)</param>
    /// <returns>Payment statistics with success/failure breakdown</returns>
    [HttpGet("payments")]
    [ProducesResponseType(typeof(ResponseModel), 200)]
    public async Task<IActionResult> GetPaymentStatistics(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? interval)
    {
        var query = new GetPaymentStatisticsQuery
        {
            StartDate = startDate,
            EndDate = endDate,
            Interval = interval ?? "day"
        };

        var result = await _mediator.Send(query);
        return StatusCode((int)result.Status!, result);
    }

    /// <summary>
    /// Get subscription statistics including active subscriptions, churn rate, renewal rate, and upgrade/downgrade analysis.
    /// </summary>
    /// <param name="startDate">Start date for the query period (defaults to 30 days ago)</param>
    /// <param name="endDate">End date for the query period (defaults to today)</param>
    /// <returns>Subscription lifecycle metrics and trends</returns>
    [HttpGet("subscriptions")]
    [ProducesResponseType(typeof(ResponseModel), 200)]
    public async Task<IActionResult> GetSubscriptionStatistics(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var query = new GetSubscriptionStatisticsQuery
        {
            StartDate = startDate,
            EndDate = endDate
        };

        var result = await _mediator.Send(query);
        return StatusCode((int)result.Status!, result);
    }

    /// <summary>
    /// Get user statistics including total users, conversion rates, lifetime value, and quota usage analysis.
    /// </summary>
    /// <param name="startDate">Start date for the query period (defaults to 30 days ago)</param>
    /// <param name="endDate">End date for the query period (defaults to today)</param>
    /// <param name="quotaThreshold">Percentage threshold for identifying users near quota limit (defaults to 80)</param>
    /// <returns>User growth metrics and engagement analysis</returns>
    [HttpGet("users")]
    [ProducesResponseType(typeof(ResponseModel), 200)]
    public async Task<IActionResult> GetUserStatistics(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int quotaThreshold = 80)
    {
        var query = new GetUserStatisticsQuery
        {
            StartDate = startDate,
            EndDate = endDate,
            QuotaThreshold = quotaThreshold
        };

        var result = await _mediator.Send(query);
        return StatusCode((int)result.Status!, result);
    }

    /// <summary>
    /// Get complete dashboard overview with all statistics aggregated in a single call.
    /// Includes revenue, payments, subscriptions, and user metrics.
    /// </summary>
    /// <param name="startDate">Start date for the query period (defaults to 30 days ago)</param>
    /// <param name="endDate">End date for the query period (defaults to today)</param>
    /// <param name="interval">Grouping interval: day, week, or month (defaults to day)</param>
    /// <returns>Comprehensive dashboard data with all metrics</returns>
    [HttpGet("overview")]
    [ProducesResponseType(typeof(ResponseModel), 200)]
    public async Task<IActionResult> GetDashboardOverview(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? interval)
    {
        var query = new GetDashboardOverviewQuery
        {
            StartDate = startDate,
            EndDate = endDate,
            Interval = interval ?? "day"
        };

        var result = await _mediator.Send(query);
        return StatusCode((int)result.Status!, result);
    }
}
