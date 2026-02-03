using ClassroomService.Application.Features.CourseRequests.DTOs;
using ClassroomService.Domain.Interfaces;
using MediatR;

namespace ClassroomService.Application.Features.CourseRequests.Queries;

public class GetMyCourseRequestsQueryHandler : IRequestHandler<GetMyCourseRequestsQuery, GetMyCourseRequestsResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKafkaUserService _userService;
    private readonly ILogger<GetMyCourseRequestsQueryHandler> _logger;

    public GetMyCourseRequestsQueryHandler(
        IUnitOfWork unitOfWork,
        IKafkaUserService userService,
        ILogger<GetMyCourseRequestsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _userService = userService;
        _logger = logger;
    }

    public async Task<GetMyCourseRequestsResponse> Handle(GetMyCourseRequestsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Get all course requests for this lecturer
            var allRequests = await _unitOfWork.CourseRequests.GetRequestsByLecturerAsync(request.LecturerId, cancellationToken);
            
            var query = allRequests.AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(request.Filter.CourseCode))
            {
                query = query.Where(cr => cr.CourseCode.Code.Contains(request.Filter.CourseCode));
            }

            if (!string.IsNullOrWhiteSpace(request.Filter.Term))
            {
                query = query.Where(cr => cr.Term.Name.Contains(request.Filter.Term));
            }

            if (!string.IsNullOrEmpty(request.Filter.Department))
            {
                query = query.Where(cr => cr.CourseCode.Department.Contains(request.Filter.Department));
            }

            if (request.Filter.CreatedAfter.HasValue)
            {
                query = query.Where(cr => cr.CreatedAt >= request.Filter.CreatedAfter.Value);
            }

            if (request.Filter.CreatedBefore.HasValue)
            {
                query = query.Where(cr => cr.CreatedAt <= request.Filter.CreatedBefore.Value);
            }

            // Get total count before pagination
            var totalCount = query.Count();

            // Apply sorting
            query = request.Filter.SortBy.ToLower() switch
            {
                "coursecode" => request.Filter.SortDirection.ToLower() == "desc"
                    ? query.OrderByDescending(cr => cr.CourseCode.Code)
                    : query.OrderBy(cr => cr.CourseCode.Code),
                "term" => request.Filter.SortDirection.ToLower() == "desc"
                    ? query.OrderByDescending(cr => cr.Term.Name)
                    : query.OrderBy(cr => cr.Term.Name),
                "status" => request.Filter.SortDirection.ToLower() == "desc"
                    ? query.OrderByDescending(cr => cr.Status)
                    : query.OrderBy(cr => cr.Status),
                _ => request.Filter.SortDirection.ToLower() == "desc"
                    ? query.OrderByDescending(cr => cr.CreatedAt)
                    : query.OrderBy(cr => cr.CreatedAt)
            };

            // Apply pagination
            var courseRequests = query
                .Skip((request.Filter.Page - 1) * request.Filter.PageSize)
                .Take(request.Filter.PageSize)
                .ToList();

            if (!courseRequests.Any())
            {
                return new GetMyCourseRequestsResponse
                {
                    Success = true,
                    Message = "No course requests found",
                    CourseRequests = new List<CourseRequestDto>(),
                    TotalCount = 0,
                    CurrentPage = request.Filter.Page,
                    PageSize = request.Filter.PageSize,
                    TotalPages = 0,
                    HasPreviousPage = false,
                    HasNextPage = false
                };
            }

            // Get user information for processors and lecturer
            var processorIds = courseRequests.Where(cr => cr.ProcessedBy.HasValue)
                .Select(cr => cr.ProcessedBy!.Value).Distinct();
            var allUserIds = processorIds.Append(request.LecturerId).Distinct();

            var users = await _userService.GetUsersByIdsAsync(allUserIds, cancellationToken);
            var userDict = users.ToDictionary(u => u.Id, u => u);

            // Get lecturer name
            var lecturerName = userDict.TryGetValue(request.LecturerId, out var lecturer)
                ? $"{lecturer.LastName} {lecturer.FirstName}".Trim()
                : "Unknown Lecturer";

            // Create DTOs
            var courseRequestDtos = courseRequests.Select(cr =>
            {
                var processedByName = cr.ProcessedBy.HasValue && userDict.TryGetValue(cr.ProcessedBy.Value, out var processor)
                    ? $"{processor.LastName} {processor.FirstName}".Trim()
                    : null;

                return new CourseRequestDto
                {
                    Id = cr.Id,
                    CourseCodeId = cr.CourseCodeId,
                    CourseCode = cr.CourseCode.Code,
                    CourseCodeTitle = cr.CourseCode.Title,
                    Description = cr.Description,
                    Term = cr.Term.Name,
                    LecturerId = cr.LecturerId,
                    LecturerName = lecturerName,
                    Status = cr.Status,
                    RequestReason = cr.RequestReason,
                    ProcessedBy = cr.ProcessedBy,
                    ProcessedByName = processedByName,
                    ProcessedAt = cr.ProcessedAt,
                    ProcessingComments = cr.ProcessingComments,
                    CreatedCourseId = cr.CreatedCourseId,
                    Announcement = cr.Announcement,
                    SyllabusFile = cr.SyllabusFile,
                    CreatedAt = cr.CreatedAt,
                    Department = cr.CourseCode.Department
                };
            }).ToList();

            var totalPages = (int)Math.Ceiling((double)totalCount / request.Filter.PageSize);

            return new GetMyCourseRequestsResponse
            {
                Success = true,
                Message = $"Successfully retrieved {courseRequestDtos.Count} course requests (page {request.Filter.Page} of {totalPages})",
                CourseRequests = courseRequestDtos,
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
            _logger.LogError(ex, "Error retrieving course requests for lecturer {LecturerId}: {ErrorMessage}",
                request.LecturerId, ex.Message);
            return new GetMyCourseRequestsResponse
            {
                Success = false,
                Message = $"Error retrieving course requests: {ex.Message}",
                CourseRequests = new List<CourseRequestDto>(),
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