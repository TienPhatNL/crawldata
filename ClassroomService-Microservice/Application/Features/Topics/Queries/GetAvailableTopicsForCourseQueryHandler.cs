using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ClassroomService.Application.Features.Topics.DTOs;
using ClassroomService.Infrastructure.Persistence;

namespace ClassroomService.Application.Features.Topics.Queries;

public class GetAvailableTopicsForCourseQueryHandler : IRequestHandler<GetAvailableTopicsForCourseQuery, GetAvailableTopicsForCourseResponse>
{
    private readonly ClassroomDbContext _context;
    private readonly ILogger<GetAvailableTopicsForCourseQueryHandler> _logger;

    public GetAvailableTopicsForCourseQueryHandler(
        ClassroomDbContext context,
        ILogger<GetAvailableTopicsForCourseQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<GetAvailableTopicsForCourseResponse> Handle(
        GetAvailableTopicsForCourseQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get the course with course code
            var course = await _context.Courses
                .Include(c => c.CourseCode)
                .FirstOrDefaultAsync(c => c.Id == request.CourseId, cancellationToken);

            if (course == null)
            {
                return new GetAvailableTopicsForCourseResponse
                {
                    Success = false,
                    Message = "Course not found"
                };
            }

            // Get topics that have weights configured for this course or course code
            var topicsWithWeights = await _context.Topics
                .Where(t => t.IsActive)
                .Where(t => _context.TopicWeights
                    .Any(tw =>
                        tw.TopicId == t.Id &&
                        (tw.CourseCodeId == course.CourseCodeId ||
                         tw.SpecificCourseId == request.CourseId)))
                .Select(t => new TopicWithWeightDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Description = t.Description,
                    // Get the weight - specific course takes precedence
                    Weight = _context.TopicWeights
                        .Where(tw => tw.TopicId == t.Id)
                        .Where(tw => tw.SpecificCourseId == request.CourseId ||
                                     tw.CourseCodeId == course.CourseCodeId)
                        .OrderByDescending(tw => tw.SpecificCourseId != null) // Specific first
                        .Select(tw => tw.WeightPercentage)
                        .FirstOrDefault()
                })
                .ToListAsync(cancellationToken);

            // Check if course has custom weights (any topic weight with SpecificCourseId)
            var hasCustomWeights = await _context.TopicWeights
                .AnyAsync(tw => tw.SpecificCourseId == request.CourseId, cancellationToken);

            return new GetAvailableTopicsForCourseResponse
            {
                Success = true,
                Message = topicsWithWeights.Any()
                    ? $"Found {topicsWithWeights.Count} topic(s) configured for this course"
                    : "No topics are configured for this course. Please contact staff to set up grading topics.",
                Topics = topicsWithWeights,
                HasCustomWeights = hasCustomWeights
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available topics for course {CourseId}", request.CourseId);
            return new GetAvailableTopicsForCourseResponse
            {
                Success = false,
                Message = "An error occurred while retrieving topics"
            };
        }
    }
}
