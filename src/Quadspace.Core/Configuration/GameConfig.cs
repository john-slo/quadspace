namespace Quadspace.Core.Configuration;

/// <summary>
/// Strongly typed view of <c>game-config.json</c>. Every gameplay tuning value lives here so balance
/// can be changed without touching code. Loaded once at client startup and injected where needed.
/// </summary>
public sealed record GameConfig(
    ArenaConfig Arena,
    ShipConfig Ship,
    SphereConfig Sphere,
    ProjectileConfig Projectile,
    ScoringConfig Scoring,
    LivesConfig Lives,
    LevelsConfig Levels,
    StarfieldConfig Starfield);

/// <summary>Bounded 2D play area, in world units (pixels at 1:1 render scale).</summary>
public sealed record ArenaConfig(double Width, double Height);

/// <summary>Player ship tuning.</summary>
public sealed record ShipConfig(double Speed, double Radius, int StartLives, double InvulnerabilitySeconds);

/// <summary>Metallic sphere tuning. Spawn rate per second = level * <see cref="SpawnRatePerLevelPerSecond"/>.</summary>
public sealed record SphereConfig(double Speed, double Radius, double SpawnRatePerLevelPerSecond, double ShrinkSeconds);

/// <summary>Fired shot tuning.</summary>
public sealed record ProjectileConfig(double Speed, double Radius, int MaxOnScreen, double CooldownSeconds);

/// <summary>Scoring rules. Spheres required to clear level N = N * <see cref="SpheresPerLevelMultiplier"/>.</summary>
public sealed record ScoringConfig(int PointsPerSphere, int SpheresPerLevelMultiplier);

/// <summary>Lives rules, extra-life cadence, and rare life-sphere spawn chance (0..1).</summary>
public sealed record LivesConfig(int ExtraLifeEveryLevels, double LifeSphereSpawnChance, int MaxLives);

/// <summary>Level presentation tuning.</summary>
public sealed record LevelsConfig(double IntroSeconds);

/// <summary>Parallax "space depth-field" background tuning.</summary>
public sealed record StarfieldConfig(int Layers, int StarsPerLayer);
