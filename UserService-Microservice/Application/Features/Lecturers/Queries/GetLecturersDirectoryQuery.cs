using MediatR;
using UserService.Application.Common.Models;

namespace UserService.Application.Features.Lecturers.Queries;

public class GetLecturersDirectoryQuery : IRequest<ResponseModel>
{
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
