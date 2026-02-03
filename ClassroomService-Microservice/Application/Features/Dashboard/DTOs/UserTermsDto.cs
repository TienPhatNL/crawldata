namespace ClassroomService.Application.Features.Dashboard.DTOs;

public class UserTermsDto
{
    public Guid? CurrentTermId { get; set; }
    public List<TermSummaryDto> Terms { get; set; } = new();
}

public class TermSummaryDto
{
    public Guid TermId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public bool IsCurrent { get; set; }
    public int CourseCount { get; set; }
}
