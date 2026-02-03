using System.Net;
using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Application.Common.Models;
using UserService.Domain.Entities;
using UserService.Infrastructure.Repositories;

namespace UserService.Application.Features.Announcements.Commands;

public class UpdateAnnouncementCommandHandler : IRequestHandler<UpdateAnnouncementCommand, ResponseModel>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateAnnouncementCommandHandler> _logger;

    public UpdateAnnouncementCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<UpdateAnnouncementCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ResponseModel> Handle(UpdateAnnouncementCommand request, CancellationToken cancellationToken)
    {
        var announcement = await _unitOfWork.Announcements.GetByIdAsync(request.Id, cancellationToken);
        if (announcement == null)
            return new ResponseModel(HttpStatusCode.NotFound, "Announcement not found");

        var publishAt = request.PublishedAt?.ToUniversalTime() ?? announcement.PublishedAt;

        announcement.Title = request.Title.Trim();
        announcement.Content = request.Content.Trim();
        announcement.Audience = request.Audience;
        announcement.PublishedAt = publishAt;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Announcement {AnnouncementId} updated for {Audience}",
            announcement.Id,
            announcement.Audience);

        var responseData = new
        {
            announcement.Id,
            announcement.Title,
            announcement.Audience,
            announcement.PublishedAt
        };

        return new ResponseModel(HttpStatusCode.OK, "Announcement updated successfully", responseData);
    }
}
