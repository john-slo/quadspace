using System.Text.Json;
using Quadspace.Core.Scoring;

namespace Quadspace.Host;

/// <summary>
/// File-based score persistence: appends every submission to a per-date daily file
/// (<c>daily/YYYY-MM-DD.json</c>) and maintains a single capped <c>top100.json</c> using the pure
/// <see cref="Leaderboard"/> ranking. File access is serialized by an in-process async lock.
/// </summary>
public sealed class FileScoreStore
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _rootDirectory;
    private readonly string _dailyDirectory;
    private readonly string _topFile;
    private readonly TimeProvider _clock;

    public FileScoreStore(string rootDirectory, TimeProvider clock)
    {
        _rootDirectory = rootDirectory;
        _dailyDirectory = Path.Combine(rootDirectory, "daily");
        _topFile = Path.Combine(rootDirectory, "top100.json");
        _clock = clock;
    }

    /// <summary>Returns the highest <paramref name="count"/> entries in descending score order.</summary>
    public async Task<IReadOnlyList<ScoreEntry>> GetTopAsync(int count, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var top = await ReadTopAsync(ct);
            return count >= top.Count ? top : top.Take(count).ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Persists a submission to the daily file and the top list, returning its placement.</summary>
    public async Task<ScoreSubmissionResponse> SubmitAsync(string name, int score, CancellationToken ct = default)
    {
        var entry = new ScoreEntry(name, score, _clock.GetUtcNow());

        await _gate.WaitAsync(ct);
        try
        {
            await AppendDailyAsync(entry, ct);

            var current = await ReadTopAsync(ct);
            var result = Leaderboard.Insert(current, entry);
            await WriteTopAsync(result.Entries, ct);

            return new ScoreSubmissionResponse(result.Rank, result.Rank is not null, result.Entries);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<IReadOnlyList<ScoreEntry>> ReadTopAsync(CancellationToken ct)
    {
        if (!File.Exists(_topFile))
        {
            return [];
        }

        await using var stream = File.OpenRead(_topFile);
        var entries = await JsonSerializer.DeserializeAsync<List<ScoreEntry>>(stream, JsonOptions, ct);
        return entries ?? [];
    }

    private async Task WriteTopAsync(IReadOnlyList<ScoreEntry> entries, CancellationToken ct)
    {
        Directory.CreateDirectory(_rootDirectory);
        await using var stream = File.Create(_topFile);
        await JsonSerializer.SerializeAsync(stream, entries, JsonOptions, ct);
    }

    private async Task AppendDailyAsync(ScoreEntry entry, CancellationToken ct)
    {
        Directory.CreateDirectory(_dailyDirectory);
        var file = Path.Combine(_dailyDirectory, $"{entry.AchievedAtUtc:yyyy-MM-dd}.json");

        List<ScoreEntry> entries;
        if (File.Exists(file))
        {
            await using var read = File.OpenRead(file);
            entries = await JsonSerializer.DeserializeAsync<List<ScoreEntry>>(read, JsonOptions, ct) ?? [];
        }
        else
        {
            entries = [];
        }

        entries.Add(entry);

        await using var write = File.Create(file);
        await JsonSerializer.SerializeAsync(write, entries, JsonOptions, ct);
    }
}
