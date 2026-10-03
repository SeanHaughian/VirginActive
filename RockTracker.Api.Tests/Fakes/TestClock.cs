using System;
using RockTracker.Api.Services;

namespace RockTracker.Api.Tests.Fakes;

public class TestClock : IClock
{
    private readonly DateTimeOffset _fixedNow;

    public TestClock(DateTimeOffset fixedNow)
    {
        _fixedNow = fixedNow;
    }

    public TestClock(int year, int month, int day)
        : this(new DateTimeOffset(new DateTime(year, month, day), TimeSpan.Zero))
    {
    }

    public DateTimeOffset Now => _fixedNow;
}