using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Http;
using UserService.Application.Common.Models;

namespace UserService.Application.Features.Users.Commands;

public class UploadProfilePictureCommand : IRequest<ResponseModel>
{
    [JsonIgnore]
    public Guid UserId { get; private set; }

    [JsonIgnore]
    public IFormFile ProfilePicture { get; set; } = null!;

    public UploadProfilePictureCommand()
    {
    }

    public UploadProfilePictureCommand(Guid userId)
    {
        UserId = userId;
    }

    public void SetUserId(Guid userId)
    {
        UserId = userId;
    }
}
