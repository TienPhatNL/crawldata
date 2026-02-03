namespace WebCrawlerService.Domain.DTOs;

public class AssignmentValidationRequest
{
    public Guid AssignmentId { get; set; }
    public Guid UserId { get; set; }
}

public class AssignmentValidationResponse
{
    public bool IsValid { get; set; }
    public bool HasAccess { get; set; }
    public string? Message { get; set; }
    public AssignmentInfo? AssignmentInfo { get; set; }
}

public class AssignmentInfo
{
    public Guid AssignmentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid CourseId { get; set; }
    public bool IsGroupAssignment { get; set; }
    public DateTime? DueDate { get; set; }
}

public class GroupValidationRequest
{
    public Guid GroupId { get; set; }
    public Guid UserId { get; set; }
}

public class GroupValidationResponse
{
    public bool IsValid { get; set; }
    public bool IsMember { get; set; }
    public string? Message { get; set; }
    public GroupInfo? GroupInfo { get; set; }
}

public class GroupInfo
{
    public Guid GroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? AssignmentId { get; set; }
    public Guid CourseId { get; set; }
    public List<GroupMemberInfo> Members { get; set; } = new();
}

public class GroupMemberInfo
{
    public Guid UserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string? StudentName { get; set; }
    public string? Email { get; set; }
}
