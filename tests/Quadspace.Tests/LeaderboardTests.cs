using Quadspace.Core.Scoring;

namespace Quadspace.Tests;

public sealed class LeaderboardTests
{
    private static ScoreEntry Entry(string name, int score, int minute = 0) =>
        new(name, score, new DateTimeOffset(2026, 1, 1, 0, minute, 0, TimeSpan.Zero));

    [Fact]
    public void Insert_IntoEmptyList_PlacesAtRankOne()
    {
        var result = Leaderboard.Insert([], Entry("A", 100));

        Assert.Equal(1, result.Rank);
        Assert.Single(result.Entries);
    }

    [Fact]
    public void Insert_HigherScore_RanksAboveLower()
    {
        var current = new[] { Entry("Low", 50) };

        var result = Leaderboard.Insert(current, Entry("High", 150));

        Assert.Equal(1, result.Rank);
        Assert.Equal("High", result.Entries[0].Name);
        Assert.Equal("Low", result.Entries[1].Name);
    }

    [Fact]
    public void Insert_EqualScore_TieBreaksByEarlierTime()
    {
        var current = new[] { Entry("Early", 100, minute: 1) };

        var result = Leaderboard.Insert(current, Entry("Later", 100, minute: 5));

        Assert.Equal("Early", result.Entries[0].Name);
        Assert.Equal("Later", result.Entries[1].Name);
        Assert.Equal(2, result.Rank);
    }

    [Fact]
    public void Insert_MidScore_GetsCorrectRank()
    {
        var current = new[] { Entry("Top", 300), Entry("Bottom", 100) };

        var result = Leaderboard.Insert(current, Entry("Mid", 200));

        Assert.Equal(2, result.Rank);
        Assert.Equal(["Top", "Mid", "Bottom"], result.Entries.Select(e => e.Name));
    }

    [Fact]
    public void Insert_WhenFull_AboveLowest_EvictsLowestAndCaps()
    {
        var current = Enumerable.Range(0, Leaderboard.MaxEntries)
            .Select(i => Entry($"P{i}", 1000 - i))
            .ToArray();

        var result = Leaderboard.Insert(current, Entry("Newcomer", 5000));

        Assert.Equal(1, result.Rank);
        Assert.Equal(Leaderboard.MaxEntries, result.Entries.Count);
        Assert.Contains(result.Entries, e => e.Name == "Newcomer");
        Assert.DoesNotContain(result.Entries, e => e.Score == 1000 - (Leaderboard.MaxEntries - 1));
    }

    [Fact]
    public void Insert_WhenFull_BelowLowest_IsNotPlaced()
    {
        var current = Enumerable.Range(0, Leaderboard.MaxEntries)
            .Select(i => Entry($"P{i}", 1000 - i))
            .ToArray();

        var result = Leaderboard.Insert(current, Entry("TooLow", 1));

        Assert.Null(result.Rank);
        Assert.Equal(Leaderboard.MaxEntries, result.Entries.Count);
        Assert.DoesNotContain(result.Entries, e => e.Name == "TooLow");
    }
}
