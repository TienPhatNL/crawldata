using Microsoft.AspNetCore.Http;

namespace ClassroomService.Domain.Interfaces;

public interface IUploadService
{
    Task<string> UploadFileAsync(IFormFile file);
    Task DeleteFileAsync(string fileUrl);
}
