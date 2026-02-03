using ClassroomService.Application.Features.CourseRequests.DTOs;
using ClassroomService.Domain.Constants;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.DTOs;
using MediatR;

namespace ClassroomService.Application.Features.CourseRequests.Queries;

public class GetAllCourseRequestsQueryHandler : IRequestHandler<GetAllCourseRequestsQuery, GetAllCourseRequestsResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKafkaUserService _userService;
    private readonly ILogger<GetAllCourseRequestsQueryHandler> _logger;

    public GetAllCourseRequestsQueryHandler(
        IUnitOfWork unitOfWork,
        IKafkaUserService userService,
        ILogger<GetAllCourseRequestsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _userService = userService;
        _logger = logger;
    }

    public async Task<GetAllCourseRequestsResponse> Handle(GetAllCourseRequestsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Check authorization
            if (request.CurrentUserRole != RoleConstants.Staff && request.CurrentUserRole != RoleConstants.Admin)
            {
                return new GetAllCourseRequestsResponse
                {
                    Success = false,
                    Message = "You are not authorized to view all course requests",
                    CourseRequests = new List<CourseRequestDto>(),
                    TotalCount = 0,
                    CurrentPage = request.Filter.Page,
                    PageSize = request.Filter.PageSize,
                    TotalPages = 0,
                    HasPreviousPage = false,
                    HasNextPage = false
                };
            }

            // Load all course requests with includes
            var allRequests = await _unitOfWork.CourseRequests.GetAllAsync(
                cancellationToken,
                cr => cr.CourseCode,
                cr => cr.Term,
                cr => cr.CreatedCourse);

            // Materialize and filter
            var requestsList = allRequests
                .Where(cr => cr.CourseCode != null && cr.Term != null)
                .ToList();

            // Apply filters in-memory
            if (request.Filter.Status.HasValue)
            {
                requestsList = requestsList.Where(cr => cr.Status == request.Filter.Status.Value).ToList();
            }

            if (!string.IsNullOrEmpty(request.Filter.CourseCode))
            {
                requestsList = requestsList.Where(cr => 
                    cr.CourseCode.Code.Contains(request.Filter.CourseCode)).ToList();
            }

            if (!string.IsNullOrEmpty(request.Filter.Term))
            {
                requestsList = requestsList.Where(cr => 
                    cr.Term.Name.Contains(request.Filter.Term)).ToList();
            }

            if (!string.IsNullOrEmpty(request.Filter.Department))
            {
                requestsList = requestsList.Where(cr => 
                    cr.CourseCode.Department.Contains(request.Filter.Department)).ToList();
            }

            if (request.Filter.CreatedAfter.HasValue)
            {
                requestsList = requestsList.Where(cr => cr.CreatedAt >= request.Filter.CreatedAfter.Value).ToList();
            }

            if (request.Filter.CreatedBefore.HasValue)
            {
                requestsList = requestsList.Where(cr => cr.CreatedAt <= request.Filter.CreatedBefore.Value).ToList();
            }

            // Get total count before pagination
            var totalCount = requestsList.Count;

            // Apply sorting
            requestsList = request.Filter.SortBy.ToLower() switch
            {
                "coursecode" => request.Filter.SortDirection.ToLower() == "desc" 
                    ? requestsList.OrderByDescending(cr => cr.CourseCode.Code).ToList()
                    : requestsList.OrderBy(cr => cr.CourseCode.Code).ToList(),
                "term" => request.Filter.SortDirection.ToLower() == "desc"
                    ? requestsList.OrderByDescending(cr => cr.Term.Name).ToList()
                    : requestsList.OrderBy(cr => cr.Term.Name).ToList(),
                "status" => request.Filter.SortDirection.ToLower() == "desc"
                    ? requestsList.OrderByDescending(cr => cr.Status).ToList()
                    : requestsList.OrderBy(cr => cr.Status).ToList(),
                _ => request.Filter.SortDirection.ToLower() == "desc"
                    ? requestsList.OrderByDescending(cr => cr.CreatedAt).ToList()
                    : requestsList.OrderBy(cr => cr.CreatedAt).ToList()
            };

            // Apply pagination
            var courseRequests = requestsList
                .Skip((request.Filter.Page - 1) * request.Filter.PageSize)
                .Take(request.Filter.PageSize)
                .ToList();

            if (!courseRequests.Any())
            {
                return new GetAllCourseRequestsResponse
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

            // Get all unique user IDs (lecturers and processors)
            var lecturerIds = courseRequests.Select(cr => cr.LecturerId).Distinct();
            var processorIds = courseRequests.Where(cr => cr.ProcessedBy.HasValue)
                .Select(cr => cr.ProcessedBy!.Value).Distinct();
            var allUserIds = lecturerIds.Union(processorIds);

            // Fetch user information
            var users = await _userService.GetUsersByIdsAsync(allUserIds, cancellationToken);
            var userDict = users?.ToDictionary(u => u.Id, u => u) ?? new Dictionary<Guid, UserDto>();

            // Create DTOs with null-safe access
            var courseRequestDtos = courseRequests.Select(cr =>
            {
                var lecturerName = userDict.TryGetValue(cr.LecturerId, out var lecturer)
                    ? $"{lecturer.LastName} {lecturer.FirstName}".Trim()
                    : "Unknown Lecturer";

                var processedByName = cr.ProcessedBy.HasValue && userDict.TryGetValue(cr.ProcessedBy.Value, out var processor)
                    ? $"{processor.LastName} {processor.FirstName}".Trim()
                    : null;

                return new CourseRequestDto
                {
                    Id = cr.Id,
                    CourseCodeId = cr.CourseCodeId,
                    CourseCode = cr.CourseCode?.Code ?? "N/A",
                    CourseCodeTitle = cr.CourseCode?.Title ?? "N/A",
                    Description = cr.Description ?? string.Empty,
                    Term = cr.Term?.Name ?? "N/A",
                    LecturerId = cr.LecturerId,
                    LecturerName = lecturerName,
                    Status = cr.Status,
                    RequestReason = cr.RequestReason ?? string.Empty,
                    ProcessedBy = cr.ProcessedBy,
                    ProcessedByName = processedByName,
                    ProcessedAt = cr.ProcessedAt,
                    ProcessingComments = cr.ProcessingComments ?? string.Empty,
                    CreatedCourseId = cr.CreatedCourseId,
                    Announcement = cr.Announcement,
                    SyllabusFile = cr.SyllabusFile,
                    CreatedAt = cr.CreatedAt,
                    Department = cr.CourseCode?.Department ?? "N/A"
                };
            }).ToList();

            // Apply lecturer name filter if specified
            if (!string.IsNullOrEmpty(request.Filter.LecturerName))
            {
                courseRequestDtos = courseRequestDtos.Where(cr =>
                    cr.LecturerName.Contains(request.Filter.LecturerName, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                // Recalculate totals after lecturer name filter
                totalCount = courseRequestDtos.Count;
                courseRequestDtos = courseRequestDtos
                    .Skip((request.Filter.Page - 1) * request.Filter.PageSize)
                    .Take(request.Filter.PageSize)
                    .ToList();
            }

            var totalPages = (int)Math.Ceiling((double)totalCount / request.Filter.PageSize);

            return new GetAllCourseRequestsResponse
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
            _logger.LogError(ex, "Error retrieving course requests: {ErrorMessage}", ex.Message);
            return new GetAllCourseRequestsResponse
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