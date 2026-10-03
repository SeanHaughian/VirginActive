using System.ComponentModel.DataAnnotations;

namespace RockTracker.Api.Models;

public record UpdateRockStatusRequest
{
    public RockStatus? Status { get; init; }
}
