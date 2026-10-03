namespace RockTracker.Api.Services;

public interface IClock
{
    DateTimeOffset Now { get; }
}
