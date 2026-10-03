using RockTracker.Api.Models;

namespace RockTracker.Api.Validation;

/// <summary>
/// Bundles the route-supplied member id, the request body, and the current time
/// so <see cref="CreateRockRequestValidator"/> can validate all of them together.
/// </summary>
public sealed record CreateRockValidationContext(string MemberId, CreateRockRequest Request, DateTimeOffset Now);
