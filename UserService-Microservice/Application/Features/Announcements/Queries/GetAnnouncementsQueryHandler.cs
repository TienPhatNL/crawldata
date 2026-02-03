using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UserService.Application.Common.Models;
using UserService.Domain.Enums;
using UserService.Infrastructure.Persistence;

namespace UserService.Application.Features.Announcements.Queries;

public class GetAnnouncementsQueryHandler : IRequestHandler<GetAnnouncementsQuery, ResponseModel>
{
    private readonly UserDbContext _dbContext;
    private readonly ILogger<GetAnnouncementsQueryHandler> _logger;

    public GetAnnouncementsQueryHandler(
        UserDbContext dbContext,
        ILogger<GetAnnouncementsQueryHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ResponseModel> Handle(GetAnnouncementsQuery request, CancellationToken cancellationToken)
    {
        if (request.Page <= 0)
            request.Page = 1;

        if (request.PageSize <= 0)
            request.PageSize = 20;

        request.PageSize = Math.Min(request.PageSize, 100);

        List<AnnouncementAudience> allowedAudiences;

        if (request.Audiences is { Count: > 0 })
        {
            allowedAudiences = request.Audiences;
        }
        else
        {
            allowedAudiences = new List<AnnouncementAudience>
            {
                AnnouncementAudience.All,
                AnnouncementAudience.Students,
                AnnouncementAudience.Lecturers
            };
        }

        var query = from announcement in _dbContext.Announcements.AsNoTracking()
                    join creator in _dbContext.Users.AsNoTracking()
                        on announcement.CreatedBy equals creator.Id into creators
                    from creator in creators.DefaultIfEmpty()
                    where allowedAudiences.Contains(announcement.Audience)
                    select new { announcement, creator };

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(x =>
                EF.Functions.Like(x.announcement.Title.ToLower(), $"%{term}%") ||
                EF.Functions.Like(x.announcement.Content.ToLower(), $"%{term}%"));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.announcement.PublishedAt)
            .ThenByDescending(x => x.announcement.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new AnnouncementDto
            {
                Id = x.announcement.Id,
                Title = x.announcement.Title,
                Content = x.announcement.Content,
                Audience = x.announcement.Audience,
                PublishedAt = x.announcement.PublishedAt,
                CreatedBy = x.announcement.CreatedBy,
                CreatedByName = x.creator != null ? x.creator.FirstName + " " + x.creator.LastName : null,
                CreatorProfilePictureUrl = x.creator != null ? x.creator.ProfilePictureUrl : null
            })
            .ToListAsync(cancellationToken);

        var responseData = new AnnouncementListResponse
        {
            Page = request.Page,
            PageSize = request.PageSize,
            TotalItems = totalCount,
            Items = items
        };

        _logger.LogInformation(
            "Returned {Count} announcements for audiences: {Audiences}",
            items.Count,
            string.Join(",", allowedAudiences));

        return new ResponseModel(HttpStatusCode.OK, "Announcements retrieved", responseData);
    }
}

public class AnnouncementListResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);
    public IReadOnlyCollection<AnnouncementDto> Items { get; set; } = Array.Empty<AnnouncementDto>();
}

public class AnnouncementDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public AnnouncementAudience Audience { get; set; }
    public DateTime PublishedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public string? CreatorProfilePictureUrl { get; set; }
}
