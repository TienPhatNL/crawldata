using MediatR;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ClassroomService.Application.Features.Courses.Commands;

public class ApproveCourseCommand : IRequest<ApproveCourseResponse>
{
    [JsonIgnore]
    public Guid CourseId { get; set; }
    
    [StringLength(500)]
    public string? Comments { get; set; }
    
    [JsonIgnore]
    public Guid ApprovedBy { get; set; }
}
