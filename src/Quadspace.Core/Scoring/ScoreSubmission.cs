namespace Quadspace.Core.Scoring;

/// <summary>Client-supplied score submission (the server stamps the time).</summary>
public sealed record ScoreSubmissionRequest(string Name, int Score);

/// <summary>Result of a submission: the placement and the resulting top list.</summary>
public sealed record ScoreSubmissionResponse(int? Rank, bool Placed, IReadOnlyList<ScoreEntry> Top);

/// <summary>Pure validation/normalization for a score submission.</summary>
public static class ScoreSubmission
{
    /// <summary>Maximum accepted player-name length.</summary>
    public const int MaxNameLength = 50;

    /// <summary>
    /// Trims and validates a submission. Returns <c>true</c> with the normalized name when valid;
    /// otherwise <c>false</c> with a human-readable <paramref name="error"/>.
    /// </summary>
    public static bool TryNormalize(string? name, int score, out string normalizedName, out string? error)
    {
        normalizedName = (name ?? string.Empty).Trim();

        if (normalizedName.Length == 0)
        {
            error = "Name is required.";
            return false;
        }

        if (normalizedName.Length > MaxNameLength)
        {
            error = $"Name must be {MaxNameLength} characters or fewer.";
            return false;
        }

        if (score < 0)
        {
            error = "Score must be non-negative.";
            return false;
        }

        error = null;
        return true;
    }
}
