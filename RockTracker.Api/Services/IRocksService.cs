using System.Threading;
using System.Threading.Tasks;
using RockTracker.Api.Models;

namespace RockTracker.Api.Services;

public interface IRocksService
{
    Task<RockOperationResult> CreateRockAsync(string memberId, CreateRockRequest request, CancellationToken cancellationToken);

    IEnumerable<Rock> GetRocks(string memberId, RockStatus? status);

    RockOperationResult UpdateStatus(string memberId, Guid rockId, UpdateRockStatusRequest? request);
}
