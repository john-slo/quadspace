using Quadspace.Core.Configuration;
using Quadspace.Core.Engine;

namespace Quadspace.Tests;

public sealed class LevelsAndLivesTests
{
    private static GameConfig Config(
        double introSeconds = 0,
        int multiplier = 100,
        int extraLifeEveryLevels = 8,
        int maxLives = 9,
        int startLives = 3,
        double invuln = 2,
        double lifeChance = 0,
        double sphereSpeed = 1,
        double sphereRadius = 20,
        double shipRadius = 16,
        double arena = 1000) =>
        new(
            new ArenaConfig(arena, arena),
            new ShipConfig(Speed: 200, Radius: shipRadius, StartLives: startLives, InvulnerabilitySeconds: invuln),
            new SphereConfig(sphereSpeed, sphereRadius, SpawnRatePerLevelPerSecond: 1, ShrinkSeconds: 0.15),
            new ProjectileConfig(Speed: 300, Radius: 4, MaxOnScreen: 8, CooldownSeconds: 0.1),
            new ScoringConfig(PointsPerSphere: 8, SpheresPerLevelMultiplier: multiplier),
            new LivesConfig(extraLifeEveryLevels, lifeChance, maxLives),
            new LevelsConfig(introSeconds),
            new StarfieldConfig(Layers: 3, StarsPerLayer: 10));

    /// <summary>Spawns a sphere, parks it on the ship, and shoots it — destroying it without a ship hit.</summary>
    private static void ShootDownOneSphere(GameEngine engine)
    {
        engine.Update(1.0, 0, 0); // spawn (rate = level/sec; low speed keeps spawns away from centre)
        var s = engine.Spheres[^1];
        s.X = engine.ShipX;
        s.Y = engine.ShipY;
        s.VelocityX = 0;
        s.VelocityY = 0;
        engine.Fire(1, 0);
        engine.Update(0.001, 0, 0);
    }

    private static void ParkOnShip(Sphere s, GameEngine engine)
    {
        s.X = engine.ShipX;
        s.Y = engine.ShipY;
        s.VelocityX = 0;
        s.VelocityY = 0;
    }

    [Fact]
    public void DestroyingRequiredSpheres_AdvancesLevelAndClearsField()
    {
        var engine = new GameEngine(Config(multiplier: 1), new Random(1));

        ShootDownOneSphere(engine); // level 1 requires 1

        Assert.Equal(2, engine.Level);
        Assert.Equal(0, engine.SpheresDestroyedThisLevel);
        Assert.Empty(engine.Spheres);
    }

    [Fact]
    public void LevelUp_OnExtraLifeCadence_GrantsALife()
    {
        var engine = new GameEngine(Config(multiplier: 1, extraLifeEveryLevels: 2, startLives: 3), new Random(1));

        ShootDownOneSphere(engine); // reaches level 2 (2 % 2 == 0 -> +1 life)

        Assert.Equal(2, engine.Level);
        Assert.Equal(4, engine.Lives);
    }

    [Fact]
    public void DestroyingLifeSphere_GrantsALife()
    {
        var engine = new GameEngine(Config(lifeChance: 1.0, startLives: 3, maxLives: 9), new Random(1));

        ShootDownOneSphere(engine);

        Assert.Equal(4, engine.Lives);
        Assert.Equal(8, engine.Score);
    }

    [Fact]
    public void DestroyingLifeSphere_DoesNotExceedMaxLives()
    {
        var engine = new GameEngine(Config(lifeChance: 1.0, startLives: 9, maxLives: 9), new Random(1));

        ShootDownOneSphere(engine);

        Assert.Equal(9, engine.Lives);
    }

    [Fact]
    public void ShipCollision_CostsALife_SetsInvulnerability_AndDestroysSphere()
    {
        var engine = new GameEngine(Config(startLives: 3, invuln: 2), new Random(1));
        engine.Update(1.0, 0, 0);
        var s = engine.Spheres[^1];
        ParkOnShip(s, engine);

        engine.Update(0.001, 0, 0);

        Assert.Equal(2, engine.Lives);
        Assert.True(engine.IsShipInvulnerable);
        Assert.True(s.IsDying);
    }

    [Fact]
    public void InvulnerableShip_DoesNotLoseASecondLifeImmediately()
    {
        var engine = new GameEngine(Config(startLives: 3, invuln: 5), new Random(1));
        engine.Update(2.0, 0, 0); // spawn two spheres
        ParkOnShip(engine.Spheres[0], engine);
        ParkOnShip(engine.Spheres[1], engine);

        engine.Update(0.001, 0, 0); // first hit -> lose one life, become invulnerable
        engine.Update(0.001, 0, 0); // still invulnerable -> no further loss

        Assert.Equal(2, engine.Lives);
    }

    [Fact]
    public void RunningOutOfLives_EndsTheGameAndFreezesUpdates()
    {
        var engine = new GameEngine(Config(startLives: 1), new Random(1));
        engine.Update(1.0, 0, 0);
        ParkOnShip(engine.Spheres[^1], engine);

        engine.Update(0.001, 0, 0);
        Assert.True(engine.IsGameOver);
        Assert.Equal(0, engine.Lives);

        var scoreBefore = engine.Score;
        var spheresBefore = engine.Spheres.Count;
        engine.Update(1.0, 0, 0); // no-op once game over

        Assert.Equal(scoreBefore, engine.Score);
        Assert.Equal(spheresBefore, engine.Spheres.Count);
    }

    [Fact]
    public void LevelIntro_PausesSpawningUntilItEnds()
    {
        var engine = new GameEngine(Config(introSeconds: 1), new Random(1));
        Assert.True(engine.IsLevelIntro);

        engine.Update(0.5, 0, 0);
        Assert.True(engine.IsLevelIntro);
        Assert.Empty(engine.Spheres);

        engine.Update(0.6, 0, 0); // intro ends this tick (no spawn yet)
        Assert.False(engine.IsLevelIntro);

        engine.Update(1.0, 0, 0); // spawning resumes
        Assert.NotEmpty(engine.Spheres);
    }
}
