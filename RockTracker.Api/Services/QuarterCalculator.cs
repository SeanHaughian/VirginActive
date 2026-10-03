namespace RockTracker.Api.Services;

public static class QuarterCalculator
{
    public static int GetQuarter(DateTimeOffset date) =>
        (date.Month - 1) / 3 + 1;

    public static DateTimeOffset GetQuarterStart(DateTimeOffset date)
    {
        var quarter = GetQuarter(date);
        var quarterStartMonth = (quarter - 1) * 3 + 1;
        return new DateTimeOffset(new DateTime(date.Year, quarterStartMonth, 1), TimeSpan.Zero);
    }

    public static DateTimeOffset GetQuarterEnd(DateTimeOffset date)
    {
        var quarterStart = GetQuarterStart(date);
        return quarterStart.AddMonths(3).AddTicks(-1);
    }

    public static bool IsWithinQuarter(DateTimeOffset date, DateTimeOffset quarterDate)
    {
        var quarterStart = GetQuarterStart(quarterDate);
        var quarterEnd = GetQuarterEnd(quarterDate);
        return date >= quarterStart && date <= quarterEnd;
    }
}