using MediatR;
using Microsoft.EntityFrameworkCore;
using ClassroomService.Infrastructure.Persistence;
using ClassroomService.Application.Features.TopicWeights.DTOs;
using ClassroomService.Domain.Common;
using ClassroomService.Domain.Interfaces;

namespace ClassroomService.Application.Features.TopicWeights.Queries;

public class GetAllTopicWeightsQueryHandler : IRequestHandler<GetAllTopicWeightsQuery, PagedResult<TopicWeightResponseDto>>
{
    private readonly ClassroomDbContext _context;
    private readonly ITopicWeightValidationService _validationService;

    public GetAllTopicWeightsQueryHandler(
        ClassroomDbContext context,
        ITopicWeightValidationService validationService)
    {
        _context = context;
        _validationService = validationService;
    }

    public async Task<PagedResult<TopicWeightResponseDto>> Handle(GetAllTopicWeightsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.TopicWeights
            .Include(tw => tw.Topic)
            .Include(tw => tw.CourseCode)
            .Include(tw => tw.SpecificCourse)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(request.CourseCode))
        {
            query = query.Where(tw => tw.CourseCode!.Code.Contains(request.CourseCode));
        }

        if (!string.IsNullOrWhiteSpace(request.TopicName))
        {
            query = query.Where(tw => tw.Topic.Name.Contains(request.TopicName));
        }

        if (!string.IsNullOrWhiteSpace(request.CourseName))
        {
            query = query.Where(tw => tw.SpecificCourse!.Name.Contains(request.CourseName));
        }

        if (request.CourseCodeId.HasValue)
        {
            query = query.Where(tw => tw.CourseCodeId == request.CourseCodeId.Value);
        }

        if (request.SpecificCourseId.HasValue)
        {
            query = query.Where(tw => tw.SpecificCourseId == request.SpecificCourseId.Value);
        }

        if (request.TopicId.HasValue)
        {
            query = query.Where(tw => tw.TopicId == request.TopicId.Value);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply ordering and pagination
        var weights = await query
            .OrderBy(tw => tw.CourseCode!.Code)
                .ThenBy(tw => tw.Topic.Name)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        // Map to DTOs with validation status
        var dtos = new List<TopicWeightResponseDto>();
        foreach (var tw in weights)
        {
            // Check if this weight can be updated/deleted
            var validation = await _validationService.ValidateUpdateAsync(tw.Id);
            
            var dto = new TopicWeightResponseDto
            {
                Id = tw.Id,
                TopicId = tw.TopicId,
                TopicName = tw.Topic.Name,
                CourseCodeId = tw.CourseCodeId,
                CourseCodeName = tw.CourseCode?.Code,
                SpecificCourseId = tw.SpecificCourseId,
                SpecificCourseName = tw.SpecificCourse?.Name,
                WeightPercentage = tw.WeightPercentage,
                Description = tw.Description,
                ConfiguredBy = tw.ConfiguredBy,
                ConfiguredAt = tw.CreatedAt,
                UpdatedAt = tw.UpdatedAt,
                CanUpdate = validation.IsValid,
                CanDelete = validation.IsValid,
                BlockReason = validation.IsValid ? null : validation.ErrorMessage
            };
            
            // Apply CanEdit filter if specified
            if (request.CanEdit.HasValue)
            {
                if (request.CanEdit.Value)
                {
                    // canEdit=true: Only show editable weights
                    if (dto.CanUpdate)
                    {
                        dtos.Add(dto);
                    }
                }
                else
                {
                    // canEdit=false: Only show non-editable weights
                    if (!dto.CanUpdate)
                    {
                        dtos.Add(dto);
                    }
                }
            }
            else
            {
                // No filter: Show all weights
                dtos.Add(dto);
            }
        }
        
        // Adjust total count to match filtered results
        var filteredCount = request.CanEdit.HasValue
            ? dtos.Count 
            : totalCount;

        return PagedResult<TopicWeightResponseDto>.Create(dtos, filteredCount, request.PageNumber, request.PageSize);
    }
}
