using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using RockTracker.Api.Models;

namespace RockTracker.Api.Services;

public class RockStore : IRockStore
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, Rock>> _store = new();
    private readonly string _filePath;
    private readonly ILogger<RockStore> _logger;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public RockStore(string filePath, ILogger<RockStore> logger)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(logger);

        _filePath = filePath;
        _logger = logger;

        try
        {
            LoadFromDisk();
            _logger.LogInformation("RockStore loaded from {path}", _filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load RockStore from disk; starting with empty store.");
        }
    }

    public Rock Create(string memberId, Rock rock)
    {
        ArgumentNullException.ThrowIfNull(memberId);
        ArgumentNullException.ThrowIfNull(rock);

        var dict = GetOrCreateMemberDict(memberId);
        dict[rock.Id] = rock;

        PersistSafely(() => PersistToDisk(), "Create operation");

        return rock;
    }

    public IEnumerable<Rock> GetAll(string memberId, RockStatus? filterStatus = null)
    {
        if (!_store.TryGetValue(memberId, out var dict))
            return Array.Empty<Rock>();

        var items = dict.Values.AsEnumerable();
        if (filterStatus.HasValue)
            items = items.Where(rock => rock.Status == filterStatus.Value);

        return items.OrderBy(rock => rock.DueDate).ToArray();
    }

    public bool TryGet(string memberId, Guid rockId, out Rock? rock)
    {
        rock = null;
        if (!_store.TryGetValue(memberId, out var dict))
            return false;

        return dict.TryGetValue(rockId, out rock);
    }

    public bool TryUpdateStatus(string memberId, Guid rockId, RockStatus newStatus, out Rock? updated, out string? error)
    {
        updated = null;
        error = null;

        if (!TryGet(memberId, rockId, out var rock))
        {
            error = "Rock not found";
            return false;
        }

        if (rock.Status != RockStatus.Pending)
        {
            error = $"Invalid transition: only rocks with status 'pending' can be moved to another status. Current status: {rock.Status}";
            return false;
        }

        if (!IsAllowedTargetStatus(newStatus))
        {
            error = "Invalid target status. Allowed target statuses: completed, missed.";
            return false;
        }

        var updatedRock = rock! with { Status = newStatus };
        var dict = GetOrCreateMemberDict(memberId);
        dict[rockId] = updatedRock;
        updated = updatedRock;

        PersistSafely(() => PersistToDisk(), "UpdateStatus operation");

        return true;
    }

    private void LoadFromDisk()
    {
        if (!File.Exists(_filePath))
            return;

        var json = File.ReadAllText(_filePath);
        if (string.IsNullOrWhiteSpace(json))
            return;

        var options = CreateJsonOptions();
        var items = JsonSerializer.Deserialize<List<Rock>>(json, options);
        if (items == null || items.Count == 0)
            return;

        foreach (var rock in items)
        {
            var dict = GetOrCreateMemberDict(rock.MemberId);
            dict[rock.Id] = rock;
        }
    }

    private void PersistToDisk()
    {
        var snapshot = GetSnapshot();
        var options = CreateJsonOptions(writeIndented: true);
        var json = JsonSerializer.Serialize(snapshot, options);

        _writeLock.Wait();
        try
        {
            WriteAtomicFile(_filePath, json);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private ConcurrentDictionary<Guid, Rock> GetOrCreateMemberDict(string memberId)
        => _store.GetOrAdd(memberId, _ => new ConcurrentDictionary<Guid, Rock>());

    private static bool IsAllowedTargetStatus(RockStatus targetStatus)
        => targetStatus == RockStatus.Completed || targetStatus == RockStatus.Missed;

    private JsonSerializerOptions CreateJsonOptions(bool writeIndented = false)
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = writeIndented,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    private List<Rock> GetSnapshot()
        => _store.Values
            .SelectMany(memberRocks => memberRocks.Values)
            .OrderBy(rock => rock.MemberId).ThenBy(rock => rock.DueDate)
            .ToList();

    private static void WriteAtomicFile(string path, string contents)
    {
        var temp = path + ".tmp";
        File.WriteAllText(temp, contents);

        if (File.Exists(path))
            File.Replace(temp, path, null);
        else
            File.Move(temp, path);
    }

    private void PersistSafely(Action persistAction, string operation)
    {
        try
        {
            persistAction();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist RockStore after {operation}", operation);
        }
    }
}
