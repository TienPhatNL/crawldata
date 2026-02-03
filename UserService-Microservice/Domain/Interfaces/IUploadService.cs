using Microsoft.AspNetCore.Http;

namespace UserService.Domain.Interfaces;

public interface IUploadService
{
    Task<string> UploadFileAsync(IFormFile file, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default);
}
