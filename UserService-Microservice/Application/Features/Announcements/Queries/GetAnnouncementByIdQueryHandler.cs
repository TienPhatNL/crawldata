using System.Net;
using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Application.Common.Models;
using UserService.Infrastructure.Repositories;

namespace UserService.Application.Features.Announcements.Queries;

public class GetAnnouncementByIdQueryHandler : IRequestHandler<GetAnnouncementByIdQuery, ResponseModel>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetAnnouncementByIdQueryHandler> _logger;

    public GetAnnouncementByIdQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetAnnouncementByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ResponseModel> Handle(GetAnnouncementByIdQuery request, CancellationToken cancellationToken)
    {
        var announcement = await _unitOfWork.Announcements.GetByIdAsync(request.Id, cancellationToken);
        if (announcement == null)
            return new ResponseModel(HttpStatusCode.NotFound, "Announcement not found");

        var data = new
        {
            announcement.Id,
            announcement.Title,
            announcement.Content,
            announcement.Audience,
            announcement.PublishedAt,
            announcement.CreatedBy
        };

        _logger.LogInformation("Announcement {AnnouncementId} fetched", announcement.Id);

        return new ResponseModel(HttpStatusCode.OK, "Announcement fetched successfully", data);
    }
}
