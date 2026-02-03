using System;
using System.Threading;
using System.Threading.Tasks;
using UserService.Domain.Entities;

namespace UserService.Domain.Services;

public interface IQuotaSnapshotService
{
    Task<UserQuotaSnapshot> UpsertFromUserAsync(
        User user,
        string source,
        bool isOverride,
        DateTime? synchronizedAt,
        CancellationToken cancellationToken = default);
}
