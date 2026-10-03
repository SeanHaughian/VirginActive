namespace RockTracker.Api.Clients.TypiCode.Models;

public record TypiCodeEnrichedProfile
{
    public required string MemberId { get; init; }
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string Username { get; init; }
    public required string Email { get; init; }
    public required string Phone { get; init; }
    public required string Website { get; init; }
    public TypiCodeAddress? Address { get; init; }
    public TypiCodeCompany? Company { get; init; }
}
