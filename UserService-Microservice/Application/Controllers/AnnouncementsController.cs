using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.Common.Models;
using UserService.Application.Features.Announcements.Commands;
using UserService.Application.Features.Announcements.Queries;
using UserService.Domain.Enums;

namespace UserService.Application.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnnouncementsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AnnouncementsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost]
    public async Task<ActionResult<ResponseModel>> CreateAnnouncement([FromBody] CreateAnnouncementCommand command)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var adminId))
            return Unauthorized(new { message = "Invalid user token" });

        command.SetCreator(adminId);

        var response = await _mediator.Send(command);
        var status = (int)(response.Status ?? HttpStatusCode.OK);
        return StatusCode(status, response);
    }

    [Authorize(
        Roles = nameof(UserRole.Admin) + "," +
                nameof(UserRole.Student) + "," +
                nameof(UserRole.PaidUser) + "," +
                nameof(UserRole.Lecturer))]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ResponseModel>> GetAnnouncementById(Guid id)
    {
        var query = new GetAnnouncementByIdQuery
        {
            Id = id
        };

        var response = await _mediator.Send(query);
        var status = (int)(response.Status ?? HttpStatusCode.OK);
        return StatusCode(status, response);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ResponseModel>> UpdateAnnouncement(Guid id, [FromBody] UpdateAnnouncementCommand command)
    {
        command.Id = id;

        var response = await _mediator.Send(command);
        var status = (int)(response.Status ?? HttpStatusCode.OK);
        return StatusCode(status, response);
    }

    [Authorize(Roles = nameof(UserRole.Student) + "," + nameof(UserRole.PaidUser))]
    [HttpGet("students")]
    public async Task<ActionResult<ResponseModel>> GetStudentAnnouncements([FromQuery] AnnouncementFilterBase filter)
    {
        var query = new GetAnnouncementsQuery
        {
            Audiences = new List<AnnouncementAudience>
            {
                AnnouncementAudience.All,
                AnnouncementAudience.Students
            },
            Page = filter.Page,
            PageSize = filter.PageSize,
            SearchTerm = filter.SearchTerm
        };

        var response = await _mediator.Send(query);
        var status = (int)(response.Status ?? HttpStatusCode.OK);
        return StatusCode(status, response);
    }

    [Authorize(Roles = nameof(UserRole.Lecturer))]
    [HttpGet("lecturers")]
    public async Task<ActionResult<ResponseModel>> GetLecturerAnnouncements([FromQuery] AnnouncementFilterBase filter)
    {
        var query = new GetAnnouncementsQuery
        {
            Audiences = new List<AnnouncementAudience>
            {
                AnnouncementAudience.All,
                AnnouncementAudience.Lecturers
            },
            Page = filter.Page,
            PageSize = filter.PageSize,
            SearchTerm = filter.SearchTerm
        };

        var response = await _mediator.Send(query);
        var status = (int)(response.Status ?? HttpStatusCode.OK);
        return StatusCode(status, response);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet("admin")]
    public async Task<ActionResult<ResponseModel>> GetAdminAnnouncements([FromQuery] AdminAnnouncementFilter filter)
    {
        var query = new GetAnnouncementsQuery
        {
            Page = filter.Page,
            PageSize = filter.PageSize,
            SearchTerm = filter.SearchTerm
        };

        if (filter.Audience.HasValue)
        {
            query.Audiences.Add(filter.Audience.Value);
        }

        var response = await _mediator.Send(query);
        var status = (int)(response.Status ?? HttpStatusCode.OK);
        return StatusCode(status, response);
    }
}

public class AnnouncementFilterBase
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SearchTerm { get; set; }
}

public class AdminAnnouncementFilter : AnnouncementFilterBase
{
    public AnnouncementAudience? Audience { get; set; }
}
