namespace Quadspace.Core.Scoring;

/// <summary>A single leaderboard record: a player name, their score, and when it was achieved (UTC).</summary>
public sealed record ScoreEntry(string Name, int Score, DateTimeOffset AchievedAtUtc);
