using System.Text.Json;
using Quadspace.Core.Configuration;

namespace Quadspace.Tests;

public sealed class GameConfigTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task GameConfig_DeserializesFromShippedJson_AllParametersPopulated()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "game-config.json");
        await using var stream = File.OpenRead(path);

        var config = await JsonSerializer.DeserializeAsync<GameConfig>(stream, JsonOptions);

        Assert.NotNull(config);
        Assert.Equal(3, config!.Ship.StartLives);
        Assert.Equal(8, config.Scoring.PointsPerSphere);
        Assert.Equal(8, config.Scoring.SpheresPerLevelMultiplier);
        Assert.Equal(8, config.Lives.ExtraLifeEveryLevels);
        Assert.True(config.Ship.Speed > config.Sphere.Speed, "the ship must move faster than the spheres");
        Assert.True(config.Sphere.SpawnRatePerLevelPerSecond > 0);
        Assert.True(config.Projectile.Speed > 0);
        Assert.True(config.Arena is { Width: > 0, Height: > 0 });
        Assert.InRange(config.Lives.LifeSphereSpawnChance, 0.0, 1.0);
        Assert.True(config.Starfield is { Layers: > 0, StarsPerLayer: > 0 });
        Assert.True(config.Sphere.SpawnOnBeat);
        Assert.Equal(128, config.Audio.BeatsPerMinute);
        Assert.True(config.Audio.SecondLayer);
    }
}
