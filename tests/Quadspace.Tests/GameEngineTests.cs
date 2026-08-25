using Quadspace.Core.Configuration;
using Quadspace.Core.Engine;

namespace Quadspace.Tests;

public sealed class GameEngineTests
{
    private static GameConfig Config(double speed = 100, double radius = 10, double width = 1000, double height = 800, int startLives = 3) =>
        new(
            new ArenaConfig(width, height),
            new ShipConfig(speed, radius, startLives, InvulnerabilitySeconds: 2),
            new SphereConfig(Speed: 50, Radius: 10, SpawnRatePerLevelPerSecond: 1, ShrinkSeconds: 0.15),
            new ProjectileConfig(Speed: 200, Radius: 3, MaxOnScreen: 32, CooldownSeconds: 0.1),
            new ScoringConfig(PointsPerSphere: 8, SpheresPerLevelMultiplier: 8),
            new LivesConfig(ExtraLifeEveryLevels: 8, LifeSphereSpawnChance: 0.02, MaxLives: 9),
            new LevelsConfig(IntroSeconds: 2),
            new StarfieldConfig(Layers: 3, StarsPerLayer: 10));

    [Fact]
    public void New_Engine_StartsCenteredWithConfiguredState()
    {
        var engine = new GameEngine(Config(width: 1000, height: 800, startLives: 4));

        Assert.Equal(500, engine.ShipX);
        Assert.Equal(400, engine.ShipY);
        Assert.Equal(0, engine.Score);
        Assert.Equal(1, engine.Level);
        Assert.Equal(4, engine.Lives);
    }

    [Fact]
    public void Update_MoveRight_IncreasesShipXBySpeedTimesDt()
    {
        var engine = new GameEngine(Config(speed: 100));

        engine.Update(0.5, moveX: 1, moveY: 0);

        Assert.Equal(550, engine.ShipX, precision: 6);
        Assert.Equal(400, engine.ShipY, precision: 6);
    }

    [Fact]
    public void Update_MoveUp_DecreasesShipY()
    {
        var engine = new GameEngine(Config(speed: 100));

        engine.Update(1.0, moveX: 0, moveY: -1);

        Assert.Equal(300, engine.ShipY, precision: 6);
    }

    [Fact]
    public void Update_Diagonal_IsNormalizedToNotExceedSpeed()
    {
        var engine = new GameEngine(Config(speed: 100));
        var startX = engine.ShipX;
        var startY = engine.ShipY;

        engine.Update(1.0, moveX: 1, moveY: 1);

        var dx = engine.ShipX - startX;
        var dy = engine.ShipY - startY;
        var distance = Math.Sqrt((dx * dx) + (dy * dy));
        Assert.Equal(100, distance, precision: 6);
    }

    [Fact]
    public void Update_ClampsAtRightAndBottomEdges()
    {
        var engine = new GameEngine(Config(speed: 100_000, radius: 10, width: 1000, height: 800));

        engine.Update(1.0, moveX: 1, moveY: 1);

        Assert.Equal(990, engine.ShipX);
        Assert.Equal(790, engine.ShipY);
    }

    [Fact]
    public void Update_ClampsAtLeftAndTopEdges()
    {
        var engine = new GameEngine(Config(speed: 100_000, radius: 10, width: 1000, height: 800));

        engine.Update(1.0, moveX: -1, moveY: -1);

        Assert.Equal(10, engine.ShipX);
        Assert.Equal(10, engine.ShipY);
    }

    [Fact]
    public void Update_NonPositiveDt_DoesNotMove()
    {
        var engine = new GameEngine(Config(speed: 100));
        var startX = engine.ShipX;

        engine.Update(0, moveX: 1, moveY: 0);

        Assert.Equal(startX, engine.ShipX);
    }
}
