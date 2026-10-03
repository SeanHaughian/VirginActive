namespace RockTracker.Api.Models;

public enum RockStatus
{
    Pending,
    Completed,
    Missed
}

public record Rock
{
    public required Guid Id { get; init; }
    public required string MemberId { get; init; }
    public required string Title { get; init; }
    [System.Text.Json.Serialization.JsonIgnore]
    public required RockCategory Category { get; init; }

    [System.Text.Json.Serialization.JsonPropertyName("category")]
    public string CategoryName
    {
        get => Category?.Name ?? string.Empty;
        init => Category = RockCategory.FromName(value);
    }
    public required DateTimeOffset DueDate { get; init; }
    public required RockStatus Status { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public string? Note { get; init; }
}
