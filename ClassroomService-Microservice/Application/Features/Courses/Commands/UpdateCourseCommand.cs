using MediatR;
using System.ComponentModel.DataAnnotations;
using ClassroomService.Domain.Constants;

namespace ClassroomService.Application.Features.Courses.Commands
{
    /// <summary>
    /// Command to update an existing course
    /// </summary>
    public class UpdateCourseCommand : IRequest<UpdateCourseResponse>
    {
        /// <summary>
        /// The course ID to update
        /// </summary>
        [Required]
        public Guid CourseId { get; set; }

        /// <summary>
        /// Updated course description (optional)
        /// </summary>
        [StringLength(ValidationConstants.MaxCourseDescriptionLength, MinimumLength = ValidationConstants.MinCourseDescriptionLength)]
        public string? Description { get; set; }

        /// <summary>
        /// Updated term ID (optional - cannot be changed if course is Active)
        /// </summary>
        public Guid? TermId { get; set; }

        /// <summary>
        /// Updated course announcement/forum (optional)
        /// </summary>
        [StringLength(2000)]
        public string? Announcement { get; set; }
    }
}
