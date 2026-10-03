using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using RockTracker.Api.Clients.TypiCode.Models;
using RockTracker.Api.Controllers;
using RockTracker.Api.Models;
using RockTracker.Api.Services;
using RockTracker.Api.Tests.Fakes;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace RockTracker.Api.Tests.Unit;

public class RocksControllerTests
{
    private readonly RockStore _store;
    private readonly RocksController _controller;

    public RocksControllerTests()
    {
        _store = new RockStore(TempFilePath(), NullLogger<RockStore>.Instance);
        var clock = new TestClock(DateTimeOffset.UtcNow);
        var validator = new RockRequestValidator();
        var typiCodeClient = new FakeTypiCodeClient(CreateFoundProfile());
        var service = new RocksService(_store, clock, validator, typiCodeClient);
        _controller = new RocksController(service);
    }

    private static TypiCodeEnrichedProfile CreateFoundProfile() => new()
    {
        MemberId = "found",
        Id = 1,
        Name = "Test Member",
        Username = "test-member",
        Email = "test-member@example.com",
        Phone = "555-0100",
        Website = "example.com"
    };

    private static string TempFilePath() =>
        System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid() + ".json");

    private static CreateRockRequest CreateValidRockRequest(
        string title = "Test Rock",
        string? category = null,
        string? note = "Career development note",
        DateTimeOffset? dueDate = null) =>
        new()
        {
            Title = title,
            Category = category ?? RockCategory.Career.Name,
            Note = note,
            DueDate = dueDate ?? DateTimeOffset.UtcNow.AddDays(7)
        };

    private static Rock CreateValidRock(
        string memberId,
        string title = "R",
        RockCategory? category = null,
        DateTimeOffset? dueDate = null,
        RockStatus status = RockStatus.Pending) =>
        new()
        {
            Id = Guid.NewGuid(),
            MemberId = memberId,
            Title = title,
            Category = category ?? RockCategory.Health,
            DueDate = dueDate ?? DateTimeOffset.UtcNow.AddDays(2),
            Status = status
        };

    [Fact]
    public async Task CreateRock_ReturnsCreated_WhenValid()
    {
        var req = CreateValidRockRequest();

        var result = await _controller.CreateRock("member1", req, default);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var value = Assert.IsType<Rock>(created.Value);
        Assert.Equal("member1", value.MemberId);
        Assert.Equal("Test Rock", value.Title);
        Assert.Equal(RockStatus.Pending, value.Status);
    }

    [Theory]
    [InlineData("m2", "No Category", null, null, 1)]
    [InlineData("member-z", "InvalidCat", "Invalid", null, 3)]
    [InlineData("member-x", "   ", "Career", "Some note", 1)]
    [InlineData("member-y", "Past", "Health", null, -2)]
    [InlineData("", "NoMember", "Other", null, 2)]
    [InlineData("m6", null, "Other", null, 2)]
    public async Task CreateRock_ReturnsBadRequest_ForInvalidRequests(
        string memberId, string? title, string? category, string? note, int dueDateDaysOffset)
    {
        var req = new CreateRockRequest
        {
            Title = title,
            Category = category,
            Note = note,
            DueDate = DateTimeOffset.UtcNow.AddDays(dueDateDaysOffset)
        };

        var result = await _controller.CreateRock(memberId, req, default);

        var bad = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, bad.StatusCode);
        Assert.IsType<ProblemDetails>(bad.Value);
    }

    [Fact]
    public async Task CreateRock_ReturnsBadRequest_WhenDueDateIsNull()
    {
        var req = new CreateRockRequest
        {
            Title = "No Due Date",
            Category = RockCategory.Other.Name,
            Note = null,
            DueDate = null
        };

        var result = await _controller.CreateRock("member-null-due", req, default);

        var bad = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, bad.StatusCode);
        Assert.IsType<ProblemDetails>(bad.Value);
    }

    [Fact]
    public async Task CreateRock_ReturnsNotFound_WhenMemberDoesNotExist()
    {
        var validator = new RockRequestValidator();
        var clock = new TestClock(DateTimeOffset.UtcNow);
        var typiCodeClient = new FakeTypiCodeClient(null);
        var service = new RocksService(_store, clock, validator, typiCodeClient);
        var controller = new RocksController(service);
        var req = CreateValidRockRequest();

        var result = await controller.CreateRock("unknown-member", req, default);

        var notFound = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
        Assert.IsType<ProblemDetails>(notFound.Value);
    }

    [Fact]
    public async Task CreateRock_ReturnsCreated_WhenMemberLookupIsUnavailable()
    {
        var validator = new RockRequestValidator();
        var clock = new TestClock(DateTimeOffset.UtcNow);
        var typiCodeClient = FakeTypiCodeClient.Unavailable();
        var service = new RocksService(_store, clock, validator, typiCodeClient);
        var controller = new RocksController(service);
        var req = CreateValidRockRequest();

        var result = await controller.CreateRock("member-during-outage", req, default);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var value = Assert.IsType<Rock>(created.Value);
        Assert.Equal("member-during-outage", value.MemberId);
    }

    [Fact]
    public void GetRocks_ReturnsNotFound_WhenMemberHasNoRocks()
    {
        var ex = Assert.Throws<KeyNotFoundException>(
            () => _controller.GetRocks("no-rocks-member", null));

        Assert.Contains("no-rocks-member", ex.Message);
    }

    [Fact]
    public void GetRocks_DoesNotReturnRocks_BelongingToAnotherMember()
    {
        _store.Create("member-a", CreateValidRock("member-a", title: "MemberARock"));
        _store.Create("member-b", CreateValidRock("member-b", title: "MemberBRock"));

        var result = _controller.GetRocks("member-a", null);
        var ok = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsAssignableFrom<System.Collections.IEnumerable>(ok.Value).Cast<Rock>().ToList();

        var item = Assert.Single(items);
        Assert.Equal("member-a", item.MemberId);
        Assert.Equal("MemberARock", item.Title);
    }

    [Theory]
    [InlineData(null, 3)]
    [InlineData(RockStatus.Pending, 1)]
    [InlineData(RockStatus.Completed, 1)]
    [InlineData(RockStatus.Missed, 1)]
    public void GetRocks_ReturnsFilteredItems_BasedOnStatus(RockStatus? status, int expectedCount)
    {
        _store.Create("m3", CreateValidRock("m3", title: "PendingRock", status: RockStatus.Pending));
        _store.Create("m3", CreateValidRock("m3", title: "CompletedRock", status: RockStatus.Completed));
        _store.Create("m3", CreateValidRock("m3", title: "MissedRock", status: RockStatus.Missed));

        var result = _controller.GetRocks("m3", status);
        var ok = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsAssignableFrom<System.Collections.IEnumerable>(ok.Value).Cast<Rock>().ToList();

        Assert.Equal(expectedCount, items.Count);
        Assert.All(items, item => Assert.Equal("m3", item.MemberId));
        if (status.HasValue)
            Assert.All(items, item => Assert.Equal(status.Value, item.Status));
    }

    [Theory]
    [InlineData(RockStatus.Pending, RockStatus.Completed)]
    [InlineData(RockStatus.Pending, RockStatus.Missed)]
    public void UpdateStatus_ReturnsOk_ForValidTransitions(RockStatus initialStatus, RockStatus newStatus)
    {
        var rock = CreateValidRock("m4", title: "ChangeMe", category: RockCategory.Revenue, dueDate: DateTimeOffset.UtcNow.AddDays(3), status: initialStatus);
        _store.Create("m4", rock);

        var req = new UpdateRockStatusRequest { Status = newStatus };

        var result = _controller.UpdateStatus("m4", rock.Id, req);
        var ok = Assert.IsType<OkObjectResult>(result);
        var updated = Assert.IsType<Rock>(ok.Value);
        Assert.Equal(newStatus, updated.Status);
    }

    [Theory]
    [InlineData(RockStatus.Pending, RockStatus.Pending)]
    [InlineData(RockStatus.Completed, RockStatus.Completed)]
    [InlineData(RockStatus.Completed, RockStatus.Missed)]
    [InlineData(RockStatus.Completed, RockStatus.Pending)]
    [InlineData(RockStatus.Missed, RockStatus.Completed)]
    [InlineData(RockStatus.Missed, RockStatus.Pending)]
    [InlineData(RockStatus.Missed, RockStatus.Missed)]
    public void UpdateStatus_ReturnsUnprocessableEntity_ForInvalidTransitions(RockStatus initialStatus, RockStatus newStatus)
    {
        var rock = CreateValidRock("m5", title: "AlreadyDone", category: RockCategory.Other, dueDate: DateTimeOffset.UtcNow.AddDays(1), status: initialStatus);
        _store.Create("m5", rock);

        var req = new UpdateRockStatusRequest { Status = newStatus };

        var result = _controller.UpdateStatus("m5", rock.Id, req);
        var bad = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, bad.StatusCode);
        Assert.IsType<ProblemDetails>(bad.Value);
    }

    [Fact]
    public void UpdateStatus_ReturnsNotFound_WhenMissing()
    {
        var req = new UpdateRockStatusRequest { Status = RockStatus.Completed };

        var result = _controller.UpdateStatus("no-member", Guid.NewGuid(), req);
        var notFound = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
        Assert.IsType<ProblemDetails>(notFound.Value);
    }

    [Fact]
    public void UpdateStatus_ReturnsNotFound_WhenRockBelongsToDifferentMember()
    {
        var rock = CreateValidRock("member-owner", title: "OwnedByOther", status: RockStatus.Pending);
        _store.Create("member-owner", rock);

        var req = new UpdateRockStatusRequest { Status = RockStatus.Completed };

        var result = _controller.UpdateStatus("member-intruder", rock.Id, req);
        var notFound = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
        Assert.IsType<ProblemDetails>(notFound.Value);
    }

    [Fact]
    public void UpdateStatus_ReturnsBadRequest_WhenStatusIsNull()
    {
        var rock = CreateValidRock("m7", title: "NeedsStatus", status: RockStatus.Pending);
        _store.Create("m7", rock);

        var req = new UpdateRockStatusRequest { Status = null };

        var result = _controller.UpdateStatus("m7", rock.Id, req);
        var bad = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, bad.StatusCode);
        Assert.IsType<ProblemDetails>(bad.Value);
    }
}
