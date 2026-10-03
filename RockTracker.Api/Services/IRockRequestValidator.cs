using RockTracker.Api.Models;

namespace RockTracker.Api.Services;

/// <summary>
/// Validates rock create/update-status requests and status transitions.
/// </summary>
public interface IRockRequestValidator
{
    IList<string> ValidateCreate(string memberId, CreateRockRequest? request, IClock clock);

    IList<string> ValidateUpdateStatus(string memberId, Guid rockId, UpdateRockStatusRequest? request);

    IList<string> ValidateTransition(Rock? existing, RockStatus newStatus);
}
