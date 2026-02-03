using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Events;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.DTOs;
using ClassroomService.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClassroomService.Application.Features.Assignments.Commands;

public class CreateAssignmentCommandHandler : IRequestHandler<CreateAssignmentCommand, CreateAssignmentResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserInfoCacheService _cacheService;
    private readonly ClassroomDbContext _context;

    public CreateAssignmentCommandHandler(
        IUnitOfWork unitOfWork, 
        IUserInfoCacheService cacheService,
        ClassroomDbContext context)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _context = context;
    }

    public async Task<CreateAssignmentResponse> Handle(CreateAssignmentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate that the course exists
            var course = await _unitOfWork.Courses
                .GetAsync(c => c.Id == request.CourseId, cancellationToken);

            if (course == null)
            {
                return new CreateAssignmentResponse
                {
                    Success = false,
                    Message = "Course not found",
                    AssignmentId = null,
                    Assignment = null,
                    GroupsAssigned = 0
                };
            }

            // Validate that the topic exists and is active
            var topic = await _unitOfWork.Topics
                .GetAsync(t => t.Id == request.TopicId, cancellationToken);

            if (topic == null)
            {
                return new CreateAssignmentResponse
                {
                    Success = false,
                    Message = "Topic not found",
                    AssignmentId = null,
                    Assignment = null,
                    GroupsAssigned = 0
                };
            }

            if (!topic.IsActive)
            {
                return new CreateAssignmentResponse
                {
                    Success = false,
                    Message = "Cannot assign to an inactive topic. Please select an active topic.",
                    AssignmentId = null,
                    Assignment = null,
                    GroupsAssigned = 0
                };
            }

            // Validate that topic has weight configured for this course
            var courseCode = await _unitOfWork.CourseCodes.GetByIdAsync(course.CourseCodeId, cancellationToken);
            var hasWeight = await _context.TopicWeights
                .AnyAsync(tw =>
                    tw.TopicId == request.TopicId &&
                    (tw.SpecificCourseId == request.CourseId ||
                     tw.CourseCodeId == course.CourseCodeId), cancellationToken);

            if (!hasWeight)
            {
                return new CreateAssignmentResponse
                {
                    Success = false,
                    Message = $"Topic '{topic.Name}' does not have a weight configured for course code '{courseCode?.Code}'. Please contact staff to configure weights for this topic.",
                    AssignmentId = null,
                    Assignment = null,
                    GroupsAssigned = 0
                };
            }

            // SNAPSHOT THE WEIGHT: Get current weight percentage and store it on assignment
            // This preserves historical accuracy even if TopicWeight is updated later
            var topicWeight = await _context.TopicWeights
                .Where(tw => tw.TopicId == request.TopicId &&
                            (tw.SpecificCourseId == request.CourseId ||
                             tw.CourseCodeId == course.CourseCodeId))
                .OrderByDescending(tw => tw.SpecificCourseId) // Prioritize course-specific over course code
                .FirstOrDefaultAsync(cancellationToken);
            
            decimal? weightSnapshot = topicWeight?.WeightPercentage;

            // Always create assignment as Draft - use AssignGroups endpoint to assign groups later
            var assignment = new Assignment
            {
                Id = Guid.NewGuid(),
                CourseId = request.CourseId,
                TopicId = request.TopicId,
                Title = request.Title,
                Description = request.Description,
                StartDate = request.StartDate,
                DueDate = request.DueDate,
                Format = request.Format,
                Status = AssignmentStatus.Draft,
                IsGroupAssignment = request.IsGroupAssignment,
                MaxPoints = request.MaxPoints,
                WeightPercentageSnapshot = weightSnapshot, // SNAPSHOT: Store current weight
                CreatedAt = DateTime.UtcNow
            };

            // Add domain event - only notify the lecturer who created the assignment
            assignment.AddDomainEvent(new AssignmentCreatedEvent(
                assignment.Id,
                assignment.CourseId,
                assignment.Title,
                assignment.DueDate,
                course.LecturerId));

            await _unitOfWork.Assignments.AddAsync(assignment);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Parse attachments
            var attachments = AssignmentAttachmentsMetadata.FromJson(assignment.Attachments);

            // Build detailed DTO
            var assignmentDto = new DTOs.AssignmentDetailDto
            {
                Id = assignment.Id,
                CourseId = assignment.CourseId,
                CourseName = course.Name,
                TopicId = assignment.TopicId,
                TopicName = topic.Name,
                Title = assignment.Title,
                Description = assignment.Description,
                StartDate = assignment.StartDate,
                DueDate = assignment.DueDate,
                Format = assignment.Format,
                Status = assignment.Status,
                StatusDisplay = assignment.Status.ToString(),
                IsGroupAssignment = assignment.IsGroupAssignment,
                MaxPoints = assignment.MaxPoints,
                IsOverdue = false,
                DaysUntilDue = (int)(assignment.DueDate - DateTime.UtcNow).TotalDays,
                AssignedGroupsCount = 0,
                Attachments = attachments.Files.Any() ? attachments.Files : null,
                CreatedAt = assignment.CreatedAt,
                UpdatedAt = assignment.UpdatedAt,
                AssignedGroups = null
            };

            // Cache assignment context for future crawler requests
            var assignmentContext = new AssignmentContextDto
            {
                Id = assignment.Id,
                Title = assignment.Title,
                Description = assignment.Description,
                Format = assignment.Format,
                DueDate = assignment.DueDate,
                ExtendedDueDate = assignment.ExtendedDueDate,
                MaxPoints = assignment.MaxPoints,
                IsGroupAssignment = assignment.IsGroupAssignment,
                CourseName = course.Name,
                TopicName = topic.Name
            };
            await _cacheService.SetAssignmentContextAsync(assignment.Id, assignmentContext, cancellationToken);

            return new CreateAssignmentResponse
            {
                Success = true,
                Message = "Assignment created successfully. Use AssignGroups endpoint to assign groups when assignment is Scheduled or Active.",
                AssignmentId = assignment.Id,
                Assignment = assignmentDto,
                GroupsAssigned = 0
            };
        }
        catch (Exception ex)
        {
            return new CreateAssignmentResponse
            {
                Success = false,
                Message = $"Error creating assignment: {ex.Message}",
                AssignmentId = null,
                Assignment = null,
                GroupsAssigned = 0
            };
        }
    }
}