using Quadspace.Core.Configuration;
using Quadspace.Core.Engine;

namespace Quadspace.Tests;

public sealed class BeatSpawnTests
{
    private static GameConfig Config(bool spawnOnBeat, double introSeconds = 0) =>
        new(
            new ArenaConfig(1000, 1000),
            new ShipConfig(Speed: 200, Radius: 16, StartLives: 3, InvulnerabilitySeconds: 2),
            new SphereConfig(Speed: 1, Radius: 20, SpawnRatePerLevelPerSecond: 1, ShrinkSeconds: 0.15, spawnOnBeat, BeatPulse: 0.18),
            new ProjectileConfig(Speed: 300, Radius: 4, MaxOnScreen: 8, CooldownSeconds: 0.1),
            new ScoringConfig(PointsPerSphere: 8, SpheresPerLevelMultiplier: 100),
            new LivesConfig(ExtraLifeEveryLevels: 8, LifeSphereSpawnChance: 0, MaxLives: 9),
            new LevelsConfig(introSeconds),
            new StarfieldConfig(Layers: 3, StarsPerLayer: 10),
            new AudioConfig(BeatsPerMinute: 128, SecondLayer: true));

    [Fact]
    public void SpawnOnBeat_Update_AccumulatesButDoesNotSpawn()
    {
        var engine = new GameEngine(Config(spawnOnBeat: true), new Random(1));

        engine.Update(1.0, 0, 0);

        Assert.Empty(engine.Spheres);
    }

    [Fact]
    public void SpawnOnBeat_OnBeat_ReleasesAccumulatedSpawns()
    {
        var engine = new GameEngine(Config(spawnOnBeat: true), new Random(1));
        engine.Update(1.0, 0, 0);

        engine.OnBeat();

        Assert.Single(engine.Spheres);
    }

    [Fact]
    public void SpawnOnBeat_OnBeat_ReleasesEveryAccumulatedSphere()
    {
        var engine = new GameEngine(Config(spawnOnBeat: true), new Random(1));
        engine.Update(3.0, 0, 0);

        engine.OnBeat();

        Assert.Equal(3, engine.Spheres.Count);
    }

    [Fact]
    public void SpawnOnBeat_OnBeatDuringLevelIntro_DoesNotSpawn()
    {
        var engine = new GameEngine(Config(spawnOnBeat: true, introSeconds: 2), new Random(1));
        engine.Update(0.5, 0, 0);

        engine.OnBeat();

        Assert.True(engine.IsLevelIntro);
        Assert.Empty(engine.Spheres);
    }

    [Fact]
    public void OnBeat_WhenSpawnOnBeatDisabled_IsANoOp()
    {
        var engine = new GameEngine(Config(spawnOnBeat: false), new Random(1));
        engine.Update(1.0, 0, 0); // spawns continuously
        var before = engine.Spheres.Count;

        engine.OnBeat();

        Assert.Equal(before, engine.Spheres.Count);
    }
}
