using MediatR;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ClassroomService.Application.Features.Courses.Commands;

public class RejectCourseCommand : IRequest<RejectCourseResponse>
{
    [JsonIgnore]
    public Guid CourseId { get; set; }
    
    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string RejectionReason { get; set; } = string.Empty;
    
    [JsonIgnore]
    public Guid RejectedBy { get; set; }
}
