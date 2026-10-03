namespace RockTracker.Api.Models;

using RockTracker.Api.Services;

public abstract class RockCategory
{
    public abstract string Name { get; }

    public abstract bool IsValid(Rock rock, DateTimeOffset now);
    public static readonly RockCategory Revenue = new RevenueCategory();
    public static readonly RockCategory Health = new HealthCategory();
    public static readonly RockCategory Career = new CareerCategory();
    public static readonly RockCategory Other = new OtherCategory();

    public static RockCategory FromName(string? name) => name?.ToLowerInvariant() switch
    {
        "revenue" => Revenue,
        "health" => Health,
        "career" => Career,
        _ => Other
    };

    private sealed class RevenueCategory : RockCategory
    {
        public override string Name => nameof(Revenue);

        public override bool IsValid(Rock rock, DateTimeOffset now)
        {
            ArgumentNullException.ThrowIfNull(rock);
            return QuarterCalculator.IsWithinQuarter(rock.DueDate, now);
        }
    }

    private sealed class HealthCategory : RockCategory
    {
        public override string Name => nameof(Health);

        public override bool IsValid(Rock rock, DateTimeOffset now)
        {
            ArgumentNullException.ThrowIfNull(rock);
            return !string.IsNullOrWhiteSpace(rock.Title) && rock.Title.Trim().Length >= 10;
        }
    }

    private sealed class CareerCategory : RockCategory
    {
        public override string Name => nameof(Career);

        public override bool IsValid(Rock rock, DateTimeOffset now)
        {
            ArgumentNullException.ThrowIfNull(rock);
            return !string.IsNullOrWhiteSpace(rock.Note);
        }
    }

    private sealed class OtherCategory : RockCategory
    {
        public override string Name => nameof(Other);

        public override bool IsValid(Rock rock, DateTimeOffset now)
        {
            ArgumentNullException.ThrowIfNull(rock);
            return true;
        }
    }
}
