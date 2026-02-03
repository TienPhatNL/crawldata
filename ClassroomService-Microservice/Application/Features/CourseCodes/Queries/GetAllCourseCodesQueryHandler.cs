using ClassroomService.Application.Features.CourseCodes.DTOs;
using ClassroomService.Domain.Constants;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;
using MediatR;

namespace ClassroomService.Application.Features.CourseCodes.Queries;

public class GetAllCourseCodesQueryHandler : IRequestHandler<GetAllCourseCodesQuery, GetAllCourseCodesResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetAllCourseCodesQueryHandler> _logger;

    public GetAllCourseCodesQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetAllCourseCodesQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GetAllCourseCodesResponse> Handle(GetAllCourseCodesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Retrieving course codes with filters");

            // Get all course codes with Courses included
            var allCourseCodes = (await _unitOfWork.CourseCodes.GetAllAsync(
                cancellationToken, 
                cc => cc.Courses)).ToList();

            // Apply filters
            var filteredCodes = allCourseCodes.AsEnumerable();

            if (!string.IsNullOrEmpty(request.Filter.Code))
            {
                filteredCodes = filteredCodes.Where(cc => cc.Code.Contains(request.Filter.Code, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(request.Filter.Title))
            {
                filteredCodes = filteredCodes.Where(cc => cc.Title.Contains(request.Filter.Title, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(request.Filter.Department))
            {
                filteredCodes = filteredCodes.Where(cc => cc.Department.Contains(request.Filter.Department, StringComparison.OrdinalIgnoreCase));
            }

            if (request.Filter.IsActive.HasValue)
            {
                filteredCodes = filteredCodes.Where(cc => cc.IsActive == request.Filter.IsActive.Value);
            }

            if (request.Filter.CreatedAfter.HasValue)
            {
                filteredCodes = filteredCodes.Where(cc => cc.CreatedAt >= request.Filter.CreatedAfter.Value);
            }

            if (request.Filter.CreatedBefore.HasValue)
            {
                filteredCodes = filteredCodes.Where(cc => cc.CreatedAt <= request.Filter.CreatedBefore.Value);
            }

            if (request.Filter.HasActiveCourses.HasValue)
            {
                if (request.Filter.HasActiveCourses.Value)
                {
                    // Only count courses with Active status
                    filteredCodes = filteredCodes.Where(cc => 
                        cc.Courses != null && cc.Courses.Any(c => c.Status == CourseStatus.Active));
                }
                else
                {
                    // No active courses (either no courses or all inactive)
                    filteredCodes = filteredCodes.Where(cc => 
                        cc.Courses == null || !cc.Courses.Any(c => c.Status == CourseStatus.Active));
                }
            }

            // Apply sorting
            var sortBy = request.Filter.SortBy?.ToLower() ?? "code";
            var sortDirection = request.Filter.SortDirection?.ToLower() ?? "asc";

            filteredCodes = sortBy switch
            {
                "title" => sortDirection == "asc" 
                    ? filteredCodes.OrderBy(cc => cc.Title) 
                    : filteredCodes.OrderByDescending(cc => cc.Title),
                "department" => sortDirection == "asc" 
                    ? filteredCodes.OrderBy(cc => cc.Department) 
                    : filteredCodes.OrderByDescending(cc => cc.Department),
                "createdat" => sortDirection == "asc" 
                    ? filteredCodes.OrderBy(cc => cc.CreatedAt) 
                    : filteredCodes.OrderByDescending(cc => cc.CreatedAt),
                "activecoursescount" => sortDirection == "asc" 
                    ? filteredCodes.OrderBy(cc => cc.Courses?.Count(c => c.Status == CourseStatus.Active) ?? 0) 
                    : filteredCodes.OrderByDescending(cc => cc.Courses?.Count(c => c.Status == CourseStatus.Active) ?? 0),
                _ => sortDirection == "asc" 
                    ? filteredCodes.OrderBy(cc => cc.Code) 
                    : filteredCodes.OrderByDescending(cc => cc.Code)
            };

            var filteredList = filteredCodes.ToList();
            var totalCount = filteredList.Count;

            // Apply pagination
            var courseCodes = filteredList
                .Skip((request.Filter.Page - 1) * request.Filter.PageSize)
                .Take(request.Filter.PageSize)
                .ToList();

            if (!courseCodes.Any() && totalCount == 0)
            {
                return new GetAllCourseCodesResponse
                {
                    Success = true,
                    Message = "No course codes found matching the criteria",
                    CourseCodes = new List<CourseCodeDto>(),
                    TotalCount = 0,
                    CurrentPage = request.Filter.Page,
                    PageSize = request.Filter.PageSize,
                    TotalPages = 0,
                    HasPreviousPage = false,
                    HasNextPage = false
                };
            }

            // Create DTOs with proper active course counting
            var courseCodeDtos = courseCodes.Select(cc => new CourseCodeDto
            {
                Id = cc.Id,
                Code = cc.Code,
                Title = cc.Title,
                Description = cc.Description,
                Department = cc.Department,
                IsActive = cc.IsActive,
                CreatedAt = cc.CreatedAt,
                UpdatedAt = cc.UpdatedAt,
                ActiveCoursesCount = cc.Courses?.Count(c => c.Status == CourseStatus.Active) ?? 0,
                TotalCoursesCount = cc.Courses?.Count ?? 0
            }).ToList();

            var totalPages = (int)Math.Ceiling((double)totalCount / request.Filter.PageSize);

            _logger.LogInformation("Retrieved {Count} course codes (page {Page} of {TotalPages})", 
                courseCodeDtos.Count, request.Filter.Page, totalPages);

            return new GetAllCourseCodesResponse
            {
                Success = true,
                Message = $"Successfully retrieved {courseCodeDtos.Count} course codes (page {request.Filter.Page} of {totalPages})",
                CourseCodes = courseCodeDtos,
                TotalCount = totalCount,
                CurrentPage = request.Filter.Page,
                PageSize = request.Filter.PageSize,
                TotalPages = totalPages,
                HasPreviousPage = request.Filter.Page > 1,
                HasNextPage = request.Filter.Page < totalPages
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving course codes: {ErrorMessage}", ex.Message);
            return new GetAllCourseCodesResponse
            {
                Success = false,
                Message = Messages.Helpers.FormatError(Messages.Error.CourseCodesRetrievalFailed, ex.Message),
                CourseCodes = new List<CourseCodeDto>(),
                TotalCount = 0,
                CurrentPage = request.Filter.Page,
                PageSize = request.Filter.PageSize,
                TotalPages = 0,
                HasPreviousPage = false,
                HasNextPage = false
            };
        }
    }
}