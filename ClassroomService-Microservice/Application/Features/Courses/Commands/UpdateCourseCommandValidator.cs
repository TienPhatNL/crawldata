using FluentValidation;
using ClassroomService.Domain.Constants;
using ClassroomService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Application.Features.Courses.Commands
{
    public class UpdateCourseCommandValidator : AbstractValidator<UpdateCourseCommand>
    {
        private readonly ClassroomDbContext _context;

        public UpdateCourseCommandValidator(ClassroomDbContext context)
        {
            _context = context;

            RuleFor(x => x.CourseId)
                .NotEmpty().WithMessage("Course ID is required");

            // TermId validation - can only be changed if course is not Active
            When(x => x.TermId.HasValue, () =>
            {
                RuleFor(x => x.TermId)
                    .NotEmpty().WithMessage("Term ID cannot be empty when provided")
                    .MustAsync(async (command, termId, cancellation) => 
                        await CanUpdateTermAsync(command.CourseId, termId))
                    .WithMessage("Cannot change term for an active course");
            });

            // Description validation (optional)
            When(x => !string.IsNullOrEmpty(x.Description), () =>
            {
                RuleFor(x => x.Description)
                    .Length(ValidationConstants.MinCourseDescriptionLength, ValidationConstants.MaxCourseDescriptionLength)
                    .WithMessage($"Course description must be between {ValidationConstants.MinCourseDescriptionLength} and {ValidationConstants.MaxCourseDescriptionLength} characters");
            });
        }

        private async Task<bool> CanUpdateTermAsync(Guid courseId, Guid? newTermId)
        {
            if (!newTermId.HasValue)
                return true;

            var course = await _context.Courses
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null)
                return true; // Let the handler deal with not found

            // If course is Active, cannot change term
            if (course.Status == CourseStatus.Active && course.TermId != newTermId.Value)
            {
                return false;
            }

            return true;
        }
    }
}
