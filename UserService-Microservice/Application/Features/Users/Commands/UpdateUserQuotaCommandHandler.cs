using System.Net;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Application.Common.Models;
using UserService.Infrastructure.Repositories;

namespace UserService.Application.Features.Users.Commands;

public class UpdateUserQuotaCommandHandler : IRequestHandler<UpdateUserQuotaCommand, ResponseModel>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateUserQuotaCommandHandler> _logger;

    public UpdateUserQuotaCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<UpdateUserQuotaCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ResponseModel> Handle(UpdateUserQuotaCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null || user.IsDeleted)
        {
            _logger.LogWarning("Quota update attempted for non-existent user {UserId}", request.UserId);
            throw new ValidationException("User not found");
        }

        var oldQuotaLimit = user.CrawlQuotaLimit;

        // Update quota limit
        user.CrawlQuotaLimit = request.NewQuotaLimit;
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Quota updated for user {UserId}: {OldLimit} -> {NewLimit}",
            user.Id, oldQuotaLimit, request.NewQuotaLimit);

        var data = new
        {
            oldQuotaLimit = oldQuotaLimit,
            newQuotaLimit = request.NewQuotaLimit
        };

        return new ResponseModel(HttpStatusCode.OK, "User quota limit updated successfully", data);
    }
}