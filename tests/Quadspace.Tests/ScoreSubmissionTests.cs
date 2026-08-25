using Quadspace.Core.Scoring;

namespace Quadspace.Tests;

public sealed class ScoreSubmissionTests
{
    [Fact]
    public void TryNormalize_ValidEntry_TrimsAndSucceeds()
    {
        var ok = ScoreSubmission.TryNormalize("  Ace  ", 120, out var name, out var error);

        Assert.True(ok);
        Assert.Equal("Ace", name);
        Assert.Null(error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TryNormalize_EmptyOrWhitespaceName_Fails(string input)
    {
        var ok = ScoreSubmission.TryNormalize(input, 10, out _, out var error);

        Assert.False(ok);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryNormalize_NameTooLong_Fails()
    {
        var tooLong = new string('x', ScoreSubmission.MaxNameLength + 1);

        var ok = ScoreSubmission.TryNormalize(tooLong, 10, out _, out var error);

        Assert.False(ok);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryNormalize_MaxLengthName_Succeeds()
    {
        var atLimit = new string('x', ScoreSubmission.MaxNameLength);

        var ok = ScoreSubmission.TryNormalize(atLimit, 10, out var name, out _);

        Assert.True(ok);
        Assert.Equal(atLimit, name);
    }

    [Fact]
    public void TryNormalize_NegativeScore_Fails()
    {
        var ok = ScoreSubmission.TryNormalize("Ace", -1, out _, out var error);

        Assert.False(ok);
        Assert.NotNull(error);
    }
}
