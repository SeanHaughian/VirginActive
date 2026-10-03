using Microsoft.AspNetCore.Http;
using RockTracker.Api.Clients.TypiCode;
using RockTracker.Api.Models;

namespace RockTracker.Api.Services;

public class RocksService : IRocksService
{
    private readonly IRockStore _store;
    private readonly IClock _clock;
    private readonly IRockRequestValidator _validator;
    private readonly ITypiCodeClient _typiCodeClient;

    public RocksService(IRockStore store, IClock clock, IRockRequestValidator validator, ITypiCodeClient typiCodeClient)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(typiCodeClient);

        _store = store;
        _clock = clock;
        _validator = validator;
        _typiCodeClient = typiCodeClient;
    }

    public async Task<RockOperationResult> CreateRockAsync(string memberId, CreateRockRequest request, CancellationToken cancellationToken)
    {
        var errors = _validator.ValidateCreate(memberId, request, _clock);
        if (errors.Count > 0)
            return RockOperationResult.Failure(StatusCodes.Status400BadRequest, "Validation Failed", errors);

        var profileResult = await _typiCodeClient.GetEnrichedProfileAsync(memberId, cancellationToken).ConfigureAwait(false);
        if (profileResult.Status == EnrichedProfileFetchStatus.NotFound)
            return RockOperationResult.Failure(StatusCodes.Status404NotFound, "Member Not Found", detail: $"No member was found with id '{memberId}'.");

        var rock = MapToRock(memberId, request);
        _store.Create(memberId, rock);

        return RockOperationResult.Success(rock);
    }

    public IEnumerable<Rock> GetRocks(string memberId, RockStatus? status)
    {
        var items = _store.GetAll(memberId, status).ToList();
        if (items.Count == 0)
            throw new KeyNotFoundException($"No member was found with id '{memberId}'.");

        return items;
    }

    public RockOperationResult UpdateStatus(string memberId, Guid rockId, UpdateRockStatusRequest? request)
    {
        var errors = _validator.ValidateUpdateStatus(memberId, rockId, request);
        if (errors.Count > 0)
            return RockOperationResult.Failure(StatusCodes.Status400BadRequest, "Validation Failed", errors);

        var newStatus = request!.Status!.Value;

        if (!_store.TryGet(memberId, rockId, out var existing))
            return RockOperationResult.Failure(StatusCodes.Status404NotFound, "Rock Not Found", detail: "Rock not found");

        var transitionErrors = _validator.ValidateTransition(existing, newStatus);
        if (transitionErrors.Count > 0)
            return RockOperationResult.Failure(StatusCodes.Status422UnprocessableEntity, "Invalid Status Transition", transitionErrors);

        var ok = _store.TryUpdateStatus(memberId, rockId, newStatus, out var updated, out var error);
        if (!ok)
        {
            if (string.Equals(error, "Rock not found", StringComparison.OrdinalIgnoreCase))
                return RockOperationResult.Failure(StatusCodes.Status404NotFound, "Rock Not Found", detail: error);

            return RockOperationResult.Failure(StatusCodes.Status422UnprocessableEntity, "Unprocessable Request", detail: error);
        }

        return RockOperationResult.Success(updated!);
    }

    private static Rock MapToRock(string memberId, CreateRockRequest request)
    {
        return new Rock
        {
            Id = Guid.NewGuid(),
            MemberId = memberId,
            Title = request.Title ?? string.Empty,
            Category = RockCategory.FromName(request.Category),
            DueDate = request.DueDate!.Value,
            Note = request.Note,
            Status = RockStatus.Pending
        };
    }
}
