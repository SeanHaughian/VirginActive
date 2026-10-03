using System;

namespace RockTracker.Api.Services;

public class SystemClock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.UtcNow;
}
