using MediatR;

namespace ClassroomService.Application.Features.Terms.Queries;

/// <summary>
/// Query to get active terms for dropdown selection
/// </summary>
public class GetTermsDropdownQuery : IRequest<GetTermsDropdownResponse>
{
}

/// <summary>
/// Response for get terms dropdown query
/// </summary>
public class GetTermsDropdownResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<TermDropdownDto> Terms { get; set; } = new();
}

/// <summary>
/// Minimal term DTO for dropdown selection
/// </summary>
public class TermDropdownDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}
