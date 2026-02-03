using System.Net;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.Common.Models;
using UserService.Application.Features.Lecturers.Queries;

namespace UserService.Application.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LecturersController : ControllerBase
{
    private readonly IMediator _mediator;

    public LecturersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<ResponseModel>> GetLecturers([FromQuery] GetLecturersDirectoryQuery query)
    {
        var response = await _mediator.Send(query);
        var status = (int)(response.Status ?? HttpStatusCode.OK);
        return StatusCode(status, response);
    }
}
