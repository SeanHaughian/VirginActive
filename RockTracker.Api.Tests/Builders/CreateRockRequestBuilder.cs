using System;
using RockTracker.Api.Models;

namespace RockTracker.Api.Tests.Builders;

public class CreateRockRequestBuilder
{
    private string? _title = "Test Rock";
    private string? _category = RockCategory.Career.Name;
    private DateTimeOffset? _dueDate = DateTimeOffset.UtcNow.AddDays(7);
    private string? _note;

    public CreateRockRequestBuilder WithTitle(string? title)
    {
        _title = title;
        return this;
    }

    public CreateRockRequestBuilder WithCategory(string? category)
    {
        _category = category;
        return this;
    }

    public CreateRockRequestBuilder WithDueDate(DateTimeOffset? dueDate)
    {
        _dueDate = dueDate;
        return this;
    }

    public CreateRockRequestBuilder WithNote(string? note)
    {
        _note = note;
        return this;
    }

    public CreateRockRequest Build() => new()
    {
        Title = _title,
        Category = _category,
        DueDate = _dueDate,
        Note = _note
    };
}
