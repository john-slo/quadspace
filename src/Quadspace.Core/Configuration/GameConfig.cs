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
    StarfieldConfig Starfield,
    AudioConfig Audio,
    ControlsConfig? Controls = null)
{
    /// <summary>
    /// Touch/mobile control tuning. Falls back to <see cref="ControlsConfig"/>'s own defaults when the
    /// optional <c>controls</c> section is omitted from the JSON, so callers get a single source of
    /// truth for the defaults instead of hard-coding them.
    /// </summary>
    public ControlsConfig ControlsOrDefault => Controls ?? new ControlsConfig();
}

/// <summary>Bounded 2D play area, in world units (pixels at 1:1 render scale).</summary>
public sealed record ArenaConfig(double Width, double Height);

/// <summary>Player ship tuning.</summary>
public sealed record ShipConfig(double Speed, double Radius, int StartLives, double InvulnerabilitySeconds);

/// <summary>
/// Metallic sphere tuning. Spawn rate per second = level * <see cref="SpawnRatePerLevelPerSecond"/>.
/// When <see cref="SpawnOnBeat"/> is true, accumulated spawns are released on the musical beat instead
/// of continuously; <see cref="BeatPulse"/> is the visual pulse amount applied on each beat.
/// </summary>
public sealed record SphereConfig(
    double Speed,
    double Radius,
    double SpawnRatePerLevelPerSecond,
    double ShrinkSeconds,
    bool SpawnOnBeat,
    double BeatPulse);

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

/// <summary>Audio tuning: the background beat tempo and whether the second melodic layer plays.</summary>
public sealed record AudioConfig(int BeatsPerMinute, bool SecondLayer);

/// <summary>
/// Touch / mobile control tuning. <see cref="JoystickDeadZone"/> is the fraction (0..1) of the
/// joystick radius ignored before the ship starts moving. When
/// <see cref="AdaptArenaToScreenOnTouch"/> is true, touch devices size the play-field to the actual
/// screen instead of the fixed <see cref="ArenaConfig"/> dimensions. This section is optional in the
/// JSON; when absent, <see cref="GameConfig.ControlsOrDefault"/> supplies an instance with the
/// default values declared below.
/// </summary>
public sealed record ControlsConfig(double JoystickDeadZone = 0.15, bool AdaptArenaToScreenOnTouch = true);
