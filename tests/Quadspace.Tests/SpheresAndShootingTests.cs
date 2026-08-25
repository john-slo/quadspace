using Quadspace.Core.Configuration;
using Quadspace.Core.Engine;

namespace Quadspace.Tests;

public sealed class SpheresAndShootingTests
{
    private static GameConfig Config(
        double arena = 1000,
        double sphereSpeed = 100,
        double sphereRadius = 20,
        double projectileSpeed = 300,
        double projectileRadius = 4,
        int maxOnScreen = 3,
        double shrinkSeconds = 0.15) =>
        new(
            new ArenaConfig(arena, arena),
            new ShipConfig(Speed: 200, Radius: 16, StartLives: 3, InvulnerabilitySeconds: 2),
            new SphereConfig(sphereSpeed, sphereRadius, SpawnRatePerLevelPerSecond: 1, shrinkSeconds),
            new ProjectileConfig(projectileSpeed, projectileRadius, maxOnScreen, CooldownSeconds: 0.1),
            new ScoringConfig(PointsPerSphere: 8, SpheresPerLevelMultiplier: 8),
            new LivesConfig(ExtraLifeEveryLevels: 8, LifeSphereSpawnChance: 0.02, MaxLives: 9),
            new LevelsConfig(IntroSeconds: 2),
            new StarfieldConfig(Layers: 3, StarsPerLayer: 10));

    [Fact]
    public void Update_AtLevelOne_SpawnsOneSpherePerSecond()
    {
        var engine = new GameEngine(Config(), new Random(1));

        engine.Update(1.0, 0, 0);

        Assert.Single(engine.Spheres);
    }

    [Fact]
    public void Update_SpawnsExpectedCountOverTime()
    {
        var engine = new GameEngine(Config(), new Random(1));

        engine.Update(1.0, 0, 0);
        engine.Update(1.0, 0, 0);
        engine.Update(1.0, 0, 0);

        Assert.Equal(3, engine.Spheres.Count);
    }

    [Fact]
    public void Update_SphereHittingRightWall_ReflectsHorizontally()
    {
        // No spawning: SpawnRate stays > 0 but we advance a tiny dt after positioning.
        var engine = new GameEngine(Config(arena: 1000, sphereSpeed: 100, sphereRadius: 20), new Random(1));
        engine.Update(1.0, 0, 0); // spawn one sphere
        var sphere = engine.Spheres[0];
        sphere.X = 985; // within radius(20) of right wall (1000)
        sphere.Y = 500;
        sphere.VelocityX = 100;
        sphere.VelocityY = 0;

        engine.Update(0.1, 0, 0);

        Assert.True(sphere.VelocityX < 0, "x velocity should invert at the wall");
        Assert.True(sphere.X <= 1000 - sphere.Radius);
    }

    [Fact]
    public void Fire_AddsProjectileInGivenDirectionAtProjectileSpeed()
    {
        var engine = new GameEngine(Config(projectileSpeed: 300), new Random(1));

        engine.Fire(1, 0);

        Assert.Single(engine.Projectiles);
        Assert.Equal(300, engine.Projectiles[0].VelocityX, precision: 6);
        Assert.Equal(0, engine.Projectiles[0].VelocityY, precision: 6);
    }

    [Fact]
    public void Fire_ZeroDirection_IsIgnored()
    {
        var engine = new GameEngine(Config(), new Random(1));

        engine.Fire(0, 0);

        Assert.Empty(engine.Projectiles);
    }

    [Fact]
    public void Fire_RespectsMaxOnScreenCap()
    {
        var engine = new GameEngine(Config(maxOnScreen: 3), new Random(1));

        for (var i = 0; i < 5; i++)
        {
            engine.Fire(0, -1);
        }

        Assert.Equal(3, engine.Projectiles.Count);
    }

    [Fact]
    public void Update_ProjectileLeavingArena_IsCulled()
    {
        var engine = new GameEngine(Config(arena: 1000, projectileSpeed: 100000), new Random(1));
        engine.Fire(1, 0);

        engine.Update(0.1, 0, 0);

        Assert.Empty(engine.Projectiles);
    }

    [Fact]
    public void Update_ProjectileHittingSphere_DestroysItAndScoresEight()
    {
        var engine = new GameEngine(Config(sphereRadius: 20, projectileRadius: 4), new Random(1));
        engine.Update(1.0, 0, 0); // spawn a sphere
        var sphere = engine.Spheres[0];
        sphere.X = engine.ShipX;
        sphere.Y = engine.ShipY;
        sphere.VelocityX = 0;
        sphere.VelocityY = 0;
        engine.Fire(1, 0); // projectile starts at the ship, overlapping the sphere

        engine.Update(0.001, 0, 0);

        Assert.Equal(8, engine.Score);
        Assert.Empty(engine.Projectiles);
        Assert.True(engine.Spheres[0].IsDying);
    }

    [Fact]
    public void Update_ScoresOnceThenRemovesSphereAfterShrink()
    {
        var engine = new GameEngine(Config(sphereRadius: 20, shrinkSeconds: 0.15), new Random(1));
        engine.Update(1.0, 0, 0);
        var sphere = engine.Spheres[0];
        sphere.X = engine.ShipX;
        sphere.Y = engine.ShipY;
        sphere.VelocityX = 0;
        sphere.VelocityY = 0;
        engine.Fire(1, 0);
        engine.Update(0.001, 0, 0); // collision -> dying

        engine.Update(0.2, 0, 0); // exceed shrinkSeconds

        Assert.Equal(8, engine.Score); // scored exactly once
        Assert.Empty(engine.Spheres);
    }
}
