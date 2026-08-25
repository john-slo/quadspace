using Quadspace.Core.Scoring;
using Quadspace.Host;

namespace Quadspace.Tests;

public sealed class FileScoreStoreTests : IDisposable
{
    private readonly string _dir;

    public FileScoreStoreTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "quadspace-tests", Guid.NewGuid().ToString("N"));
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    [Fact]
    public async Task SubmitAsync_FirstScore_PlacesAtRankOne_AndWritesDailyFile()
    {
        var now = new DateTimeOffset(2026, 3, 4, 12, 0, 0, TimeSpan.Zero);
        var store = new FileScoreStore(_dir, new FixedTimeProvider(now));

        var response = await store.SubmitAsync("Ace", 100);

        Assert.True(response.Placed);
        Assert.Equal(1, response.Rank);
        Assert.Single(response.Top);
        Assert.True(File.Exists(Path.Combine(_dir, "top100.json")));
        Assert.True(File.Exists(Path.Combine(_dir, "daily", "2026-03-04.json")));
    }

    [Fact]
    public async Task SubmitAsync_MultipleScores_AreReturnedInDescendingOrder()
    {
        var now = new DateTimeOffset(2026, 3, 4, 12, 0, 0, TimeSpan.Zero);
        var store = new FileScoreStore(_dir, new FixedTimeProvider(now));

        await store.SubmitAsync("Low", 50);
        await store.SubmitAsync("High", 300);
        await store.SubmitAsync("Mid", 150);

        var top = await store.GetTopAsync(10);

        Assert.Equal(["High", "Mid", "Low"], top.Select(e => e.Name));
    }

    [Fact]
    public async Task GetTopAsync_RespectsCount()
    {
        var store = new FileScoreStore(_dir, new FixedTimeProvider(DateTimeOffset.UnixEpoch));
        await store.SubmitAsync("A", 10);
        await store.SubmitAsync("B", 20);
        await store.SubmitAsync("C", 30);

        var top = await store.GetTopAsync(2);

        Assert.Equal(2, top.Count);
        Assert.Equal("C", top[0].Name);
        Assert.Equal("B", top[1].Name);
    }

    [Fact]
    public async Task GetTopAsync_NoData_ReturnsEmpty()
    {
        var store = new FileScoreStore(_dir, new FixedTimeProvider(DateTimeOffset.UnixEpoch));

        var top = await store.GetTopAsync(10);

        Assert.Empty(top);
    }

    [Fact]
    public async Task SubmitAsync_PersistsAcrossStoreInstances()
    {
        var clock = new FixedTimeProvider(DateTimeOffset.UnixEpoch);
        await new FileScoreStore(_dir, clock).SubmitAsync("Persisted", 42);

        var reopened = await new FileScoreStore(_dir, clock).GetTopAsync(10);

        Assert.Single(reopened);
        Assert.Equal("Persisted", reopened[0].Name);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }
}
