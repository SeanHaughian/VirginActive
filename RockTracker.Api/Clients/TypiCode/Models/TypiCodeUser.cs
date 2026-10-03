namespace RockTracker.Api.Clients.TypiCode.Models;

public record TypiCodeUser
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string Username { get; init; }
    public required string Email { get; init; }
    public required string Phone { get; init; }
    public required string Website { get; init; }
    public TypiCodeAddress? Address { get; init; }
    public TypiCodeCompany? Company { get; init; }
}

public record TypiCodeAddress
{
    public required string Street { get; init; }
    public required string Suite { get; init; }
    public required string City { get; init; }
    public required string Zipcode { get; init; }
    public TypiCodeGeo? Geo { get; init; }
}

public record TypiCodeGeo
{
    public required string Lat { get; init; }
    public required string Lng { get; init; }
}

public record TypiCodeCompany
{
    public required string Name { get; init; }
    public required string CatchPhrase { get; init; }
    public required string Bs { get; init; }
}
