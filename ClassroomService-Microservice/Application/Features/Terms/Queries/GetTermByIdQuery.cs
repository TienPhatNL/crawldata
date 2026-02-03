using MediatR;
using ClassroomService.Application.Features.Terms.DTOs;

namespace ClassroomService.Application.Features.Terms.Queries;

/// <summary>
/// Query to get a term by ID
/// </summary>
public class GetTermByIdQuery : IRequest<GetTermByIdResponse>
{
    /// <summary>
    /// Term ID
    /// </summary>
    public Guid Id { get; set; }
}

/// <summary>
/// Response for get term by ID query
/// </summary>
public class GetTermByIdResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public TermDto? Term { get; set; }
}
