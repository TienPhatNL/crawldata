using System.Net;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UserService.Application.Common.Models;
using UserService.Domain.Enums;
using UserService.Infrastructure.Persistence;

namespace UserService.Application.Features.Lecturers.Queries;

public class GetLecturersDirectoryQueryHandler : IRequestHandler<GetLecturersDirectoryQuery, ResponseModel>
{
    private readonly UserDbContext _dbContext;
    private readonly ILogger<GetLecturersDirectoryQueryHandler> _logger;

    public GetLecturersDirectoryQueryHandler(
        UserDbContext dbContext,
        ILogger<GetLecturersDirectoryQueryHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ResponseModel> Handle(GetLecturersDirectoryQuery request, CancellationToken cancellationToken)
    {
        if (request.Page <= 0)
        {
            request.Page = 1;
        }

        if (request.PageSize <= 0)
        {
            request.PageSize = 50;
        }

        request.PageSize = Math.Min(request.PageSize, 100);

        var query = _dbContext.Users
            .AsNoTracking()
            .Where(u => u.Role == UserRole.Lecturer && u.Status == UserStatus.Active);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(u =>
                EF.Functions.Like(u.FirstName.ToLower(), $"%{term}%") ||
                EF.Functions.Like(u.LastName.ToLower(), $"%{term}%") ||
                (u.InstitutionName != null && EF.Functions.Like(u.InstitutionName.ToLower(), $"%{term}%")));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var lecturers = await query
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new LecturerDirectoryItem
            {
                Id = u.Id,
                FullName = u.LastName + " " + u.FirstName,
                ProfilePictureUrl = u.ProfilePictureUrl,
                InstitutionName = u.InstitutionName
            })
            .ToListAsync(cancellationToken);

        var responseData = new LecturerDirectoryResponse
        {
            Page = request.Page,
            PageSize = request.PageSize,
            TotalItems = totalCount,
            Items = lecturers
        };

        _logger.LogInformation("Returned {Count} lecturers (Page {Page})", lecturers.Count, request.Page);

        return new ResponseModel(HttpStatusCode.OK, "Lecturers retrieved", responseData);
    }
}

public class LecturerDirectoryResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);
    public IReadOnlyCollection<LecturerDirectoryItem> Items { get; set; } = Array.Empty<LecturerDirectoryItem>();
}

public class LecturerDirectoryItem
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public string? InstitutionName { get; set; }
}
