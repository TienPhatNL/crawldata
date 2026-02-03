using System.Net;
using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Application.Common.Models;
using UserService.Domain.Entities;
using UserService.Infrastructure.Repositories;

namespace UserService.Application.Features.Announcements.Commands;

public class CreateAnnouncementCommandHandler : IRequestHandler<CreateAnnouncementCommand, ResponseModel>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateAnnouncementCommandHandler> _logger;

    public CreateAnnouncementCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<CreateAnnouncementCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ResponseModel> Handle(CreateAnnouncementCommand request, CancellationToken cancellationToken)
    {
        var publishAt = request.PublishedAt?.ToUniversalTime() ?? DateTime.UtcNow;

        var announcement = new Announcement
        {
            Title = request.Title.Trim(),
            Content = request.Content.Trim(),
            Audience = request.Audience,
            CreatedBy = request.CreatedBy,
            PublishedAt = publishAt
        };

        await _unitOfWork.Announcements.AddAsync(announcement, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Announcement {AnnouncementId} created for {Audience} by {Creator}",
            announcement.Id,
            announcement.Audience,
            announcement.CreatedBy);

        var responseData = new
        {
            announcement.Id,
            announcement.Title,
            announcement.Audience,
            announcement.PublishedAt
        };

        return new ResponseModel(HttpStatusCode.Created, "Announcement created successfully", responseData);
    }
}
