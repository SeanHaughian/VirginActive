using System.ComponentModel.DataAnnotations;

namespace RockTracker.Api.Models;

public record CreateRockRequest
{
    public string? Title { get; init; }

    public string? Category { get; init; }

    public DateTimeOffset? DueDate { get; init; }

    public string? Note { get; init; }
}
