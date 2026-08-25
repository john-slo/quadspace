namespace Quadspace.Core.Scoring;

/// <summary>
/// Pure, deterministic leaderboard ranking. Orders entries by score descending, breaking ties by the
/// earlier achievement time, and caps the list at a maximum number of entries.
/// </summary>
public static class Leaderboard
{
    /// <summary>The single top-list size the product keeps (the home page shows the first ten).</summary>
    public const int MaxEntries = 100;

    /// <summary>
    /// Inserts <paramref name="candidate"/> into <paramref name="current"/>, returning the new capped,
    /// ordered list and the candidate's 1-based rank (or <c>null</c> if it did not make the cut).
    /// </summary>
    public static LeaderboardInsertResult Insert(
        IReadOnlyList<ScoreEntry> current,
        ScoreEntry candidate,
        int cap = MaxEntries)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cap);

        var ordered = new List<ScoreEntry>(current.Count + 1);
        ordered.AddRange(current);
        ordered.Add(candidate);
        ordered.Sort(Compare);

        if (ordered.Count > cap)
        {
            ordered.RemoveRange(cap, ordered.Count - cap);
        }

        var rankIndex = -1;
        for (var i = 0; i < ordered.Count; i++)
        {
            if (ReferenceEquals(ordered[i], candidate))
            {
                rankIndex = i;
                break;
            }
        }

        return new LeaderboardInsertResult(ordered, rankIndex >= 0 ? rankIndex + 1 : null);
    }

    private static int Compare(ScoreEntry a, ScoreEntry b)
    {
        var byScore = b.Score.CompareTo(a.Score);
        return byScore != 0 ? byScore : a.AchievedAtUtc.CompareTo(b.AchievedAtUtc);
    }
}

/// <summary>The outcome of a <see cref="Leaderboard.Insert"/>: the capped list and the candidate rank.</summary>
public sealed record LeaderboardInsertResult(IReadOnlyList<ScoreEntry> Entries, int? Rank);
