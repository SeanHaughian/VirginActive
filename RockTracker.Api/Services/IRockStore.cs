using RockTracker.Api.Models;

namespace RockTracker.Api.Services;

public interface IRockStore
{
    Rock Create(string memberId, Rock rock);

    IEnumerable<Rock> GetAll(string memberId, RockStatus? filterStatus = null);

    bool TryGet(string memberId, Guid rockId, out Rock? rock);

    bool TryUpdateStatus(string memberId, Guid rockId, RockStatus newStatus, out Rock? updated, out string? error);
}
