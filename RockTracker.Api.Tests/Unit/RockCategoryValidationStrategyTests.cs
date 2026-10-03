using System;
using RockTracker.Api.Models;
using Xunit;

namespace RockTracker.Api.Tests.Unit;

public class RockCategoryValidationStrategyTests
{
    private static Rock CreateRock(RockCategory category, string title = "Default title here", string? note = null, DateTimeOffset? dueDate = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            MemberId = "member-1",
            Title = title,
            Category = category,
            DueDate = dueDate ?? DateTimeOffset.UtcNow.AddDays(5),
            Status = RockStatus.Pending,
            Note = note
        };

    [Fact]
    public void FromName_ReturnsMatchingCategory_ForEachKnownName()
    {
        Assert.Same(RockCategory.Revenue, RockCategory.FromName("Revenue"));
        Assert.Same(RockCategory.Health, RockCategory.FromName("Health"));
        Assert.Same(RockCategory.Career, RockCategory.FromName("Career"));
        Assert.Same(RockCategory.Other, RockCategory.FromName("Other"));
    }

    [Theory]
    [InlineData("revenue")]
    [InlineData("REVENUE")]
    [InlineData("ReVeNuE")]
    public void FromName_IsCaseInsensitive(string name)
    {
        Assert.Same(RockCategory.Revenue, RockCategory.FromName(name));
    }

    [Fact]
    public void FromName_DefaultsToOther_ForUnknownOrNullName()
    {
        Assert.Same(RockCategory.Other, RockCategory.FromName("NotACategory"));
        Assert.Same(RockCategory.Other, RockCategory.FromName(null));
    }

    [Theory]
    [InlineData(0, true)]   // today: start of current quarter
    [InlineData(45, true)]  // mid-quarter
    [InlineData(89, true)]  // last day of quarter (quarters are ~90 days)
    [InlineData(-1, false)] // one day before the quarter started
    [InlineData(100, false)] // past the end of the quarter
    public void Revenue_IsValid_EnforcesDueDateWithinCurrentQuarter(int daysFromQuarterStart, bool expectedValid)
    {
        var now = new DateTimeOffset(new DateTime(2024, 1, 1), TimeSpan.Zero);
        var quarterStart = Services.QuarterCalculator.GetQuarterStart(now);
        var dueDate = quarterStart.AddDays(daysFromQuarterStart);

        var rock = CreateRock(RockCategory.Revenue, dueDate: dueDate);

        Assert.Equal(expectedValid, RockCategory.Revenue.IsValid(rock, now));
    }

    [Fact]
    public void Revenue_IsValid_ThrowsArgumentNullException_WhenRockIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => RockCategory.Revenue.IsValid(null!, DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData("ShortTitl", false)]      // 9 characters
    [InlineData("ShortTitle", true)]      // exactly 10 characters
    [InlineData("A Much Longer Health Title", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public void Health_IsValid_EnforcesMinimumTitleLength(string title, bool expectedValid)
    {
        var rock = CreateRock(RockCategory.Health, title: title);

        Assert.Equal(expectedValid, RockCategory.Health.IsValid(rock, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Health_IsValid_TrimsWhitespace_BeforeCountingLength()
    {
        var rock = CreateRock(RockCategory.Health, title: "  Short  ");

        Assert.False(RockCategory.Health.IsValid(rock, DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("This explains why it matters", true)]
    public void Career_IsValid_RequiresNonEmptyNote(string? note, bool expectedValid)
    {
        var rock = CreateRock(RockCategory.Career, note: note);

        Assert.Equal(expectedValid, RockCategory.Career.IsValid(rock, DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Any title works, no extra rules apply")]
    public void Other_IsValid_AlwaysReturnsTrue_RegardlessOfNoteOrTitle(string? note)
    {
        var rock = CreateRock(RockCategory.Other, note: note);

        Assert.True(RockCategory.Other.IsValid(rock, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void EachCategory_HasDistinctName_MatchingItsEnumLikeIdentity()
    {
        Assert.Equal("Revenue", RockCategory.Revenue.Name);
        Assert.Equal("Health", RockCategory.Health.Name);
        Assert.Equal("Career", RockCategory.Career.Name);
        Assert.Equal("Other", RockCategory.Other.Name);
    }
}