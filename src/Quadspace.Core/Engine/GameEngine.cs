using Quadspace.Core.Configuration;

namespace Quadspace.Core.Engine;

/// <summary>
/// Deterministic, side-effect-free game simulation. Owns the authoritative game state (ship, spheres,
/// projectiles, score, level, lives) and advances it one frame at a time via <see cref="Update"/>.
/// Rendering, input, and timing live outside (JS interop). A seeded <see cref="Random"/> can be
/// injected for testability.
/// </summary>
public sealed class GameEngine
{
    private readonly GameConfig _config;
    private readonly Random _rng;
    private readonly List<Sphere> _spheres = [];
    private readonly List<Projectile> _projectiles = [];
    private double _spawnAccumulator;

    public GameEngine(GameConfig config, Random? rng = null)
    {
        ArgumentNullException.ThrowIfNull(config);
        _config = config;
        _rng = rng ?? new Random();
        ShipX = config.Arena.Width / 2;
        ShipY = config.Arena.Height / 2;
        Lives = config.Ship.StartLives;
        LevelIntroRemaining = config.Levels.IntroSeconds;
    }

    /// <summary>Ship centre X in arena units.</summary>
    public double ShipX { get; private set; }

    /// <summary>Ship centre Y in arena units.</summary>
    public double ShipY { get; private set; }

    /// <summary>Current score.</summary>
    public int Score { get; private set; }

    /// <summary>Current level (1-based).</summary>
    public int Level { get; private set; } = 1;

    /// <summary>Remaining lives.</summary>
    public int Lives { get; private set; }

    /// <summary>Spheres shot down toward the current level's target.</summary>
    public int SpheresDestroyedThisLevel { get; private set; }

    /// <summary>Spheres that must be destroyed to clear the current level (level * multiplier).</summary>
    public int SpheresRequiredThisLevel => Level * _config.Scoring.SpheresPerLevelMultiplier;

    /// <summary>Seconds left on the current level-intro banner (0 = playing).</summary>
    public double LevelIntroRemaining { get; private set; }

    /// <summary>True while the level-intro banner is showing (gameplay spawning is paused).</summary>
    public bool IsLevelIntro => LevelIntroRemaining > 0;

    /// <summary>Seconds left of post-hit invulnerability (0 = vulnerable).</summary>
    public double InvulnerabilityRemaining { get; private set; }

    /// <summary>True while the ship is briefly invulnerable after a hit.</summary>
    public bool IsShipInvulnerable => InvulnerabilityRemaining > 0;

    /// <summary>True once lives reach zero; the simulation stops advancing.</summary>
    public bool IsGameOver { get; private set; }

    /// <summary>Live spheres in the arena (including those currently shrinking out).</summary>
    public IReadOnlyList<Sphere> Spheres => _spheres;

    /// <summary>In-flight projectiles.</summary>
    public IReadOnlyList<Projectile> Projectiles => _projectiles;

    /// <summary>
    /// Advances the simulation by <paramref name="dtSeconds"/>: moves the ship; (unless a level intro
    /// is showing) spawns spheres; moves projectiles/spheres with bounces and shrink timers; resolves
    /// projectile hits (destroy + score + life-spheres) and ship collisions (life loss + invulnerability
    /// + game over); and advances the level when its target is met.
    /// </summary>
    public void Update(double dtSeconds, double moveX, double moveY)
    {
        if (dtSeconds <= 0 || IsGameOver)
        {
            return;
        }

        MoveShip(dtSeconds, moveX, moveY);

        if (InvulnerabilityRemaining > 0)
        {
            InvulnerabilityRemaining = Math.Max(0, InvulnerabilityRemaining - dtSeconds);
        }

        if (LevelIntroRemaining > 0)
        {
            LevelIntroRemaining = Math.Max(0, LevelIntroRemaining - dtSeconds);
        }
        else
        {
            SpawnSpheres(dtSeconds);
        }

        MoveProjectiles(dtSeconds);
        MoveSpheres(dtSeconds);
        ResolveProjectileHits();

        if (!IsShipInvulnerable)
        {
            ResolveShipCollisions();
        }

        CheckLevelUp();
    }

    /// <summary>Fires one shot from the ship in the given axis direction, respecting the on-screen cap.</summary>
    public void Fire(double directionX, double directionY)
    {
        if (IsGameOver)
        {
            return;
        }

        var length = Math.Sqrt((directionX * directionX) + (directionY * directionY));
        if (length <= 0 || _projectiles.Count >= _config.Projectile.MaxOnScreen)
        {
            return;
        }

        var speed = _config.Projectile.Speed;
        _projectiles.Add(new Projectile
        {
            X = ShipX,
            Y = ShipY,
            VelocityX = directionX / length * speed,
            VelocityY = directionY / length * speed,
            Radius = _config.Projectile.Radius,
        });
    }

    private void MoveShip(double dtSeconds, double moveX, double moveY)
    {
        var length = Math.Sqrt((moveX * moveX) + (moveY * moveY));
        if (length > 1)
        {
            moveX /= length;
            moveY /= length;
        }

        var distance = _config.Ship.Speed * dtSeconds;
        var radius = _config.Ship.Radius;
        ShipX = Math.Clamp(ShipX + (moveX * distance), radius, _config.Arena.Width - radius);
        ShipY = Math.Clamp(ShipY + (moveY * distance), radius, _config.Arena.Height - radius);
    }

    private void SpawnSpheres(double dtSeconds)
    {
        var rate = Level * _config.Sphere.SpawnRatePerLevelPerSecond;
        if (rate <= 0)
        {
            return;
        }

        _spawnAccumulator += dtSeconds * rate;

        // When spawns are quantized to the beat, hold them until OnBeat releases them.
        if (_config.Sphere.SpawnOnBeat)
        {
            return;
        }

        FlushSpawns();
    }

    private void FlushSpawns()
    {
        while (_spawnAccumulator >= 1)
        {
            _spawnAccumulator -= 1;
            _spheres.Add(CreateSphereFromEdge());
        }
    }

    /// <summary>
    /// Releases any accumulated beat-quantized spawns. Called from the audio layer on each beat when
    /// <c>sphere.spawnOnBeat</c> is enabled; a no-op during a level intro, game over, or when disabled.
    /// </summary>
    public void OnBeat()
    {
        if (IsGameOver || IsLevelIntro || !_config.Sphere.SpawnOnBeat)
        {
            return;
        }

        FlushSpawns();
    }

    private Sphere CreateSphereFromEdge()
    {
        var radius = _config.Sphere.Radius;
        var width = _config.Arena.Width;
        var height = _config.Arena.Height;
        var edge = _rng.Next(4);

        double x, y;
        switch (edge)
        {
            case 0: x = radius; y = _rng.NextDouble() * height; break;           // left
            case 1: x = width - radius; y = _rng.NextDouble() * height; break;    // right
            case 2: x = _rng.NextDouble() * width; y = radius; break;             // top
            default: x = _rng.NextDouble() * width; y = height - radius; break;   // bottom
        }

        var angle = _rng.NextDouble() * Math.PI * 2;
        var vx = Math.Cos(angle) * _config.Sphere.Speed;
        var vy = Math.Sin(angle) * _config.Sphere.Speed;

        // Ensure the initial velocity heads into the arena from the spawn edge.
        if (edge == 0 && vx < 0) { vx = -vx; }
        if (edge == 1 && vx > 0) { vx = -vx; }
        if (edge == 2 && vy < 0) { vy = -vy; }
        if (edge == 3 && vy > 0) { vy = -vy; }

        return new Sphere
        {
            X = x,
            Y = y,
            VelocityX = vx,
            VelocityY = vy,
            Radius = radius,
            IsLifeSphere = _rng.NextDouble() < _config.Lives.LifeSphereSpawnChance,
        };
    }

    private void MoveProjectiles(double dtSeconds)
    {
        var width = _config.Arena.Width;
        var height = _config.Arena.Height;
        for (var i = _projectiles.Count - 1; i >= 0; i--)
        {
            var p = _projectiles[i];
            p.X += p.VelocityX * dtSeconds;
            p.Y += p.VelocityY * dtSeconds;
            if (p.X < 0 || p.X > width || p.Y < 0 || p.Y > height)
            {
                _projectiles.RemoveAt(i);
            }
        }
    }

    private void MoveSpheres(double dtSeconds)
    {
        var width = _config.Arena.Width;
        var height = _config.Arena.Height;
        for (var i = _spheres.Count - 1; i >= 0; i--)
        {
            var s = _spheres[i];
            if (s.IsDying)
            {
                s.DyingRemaining -= dtSeconds;
                s.ShrinkFraction = Math.Max(0, s.DyingRemaining / _config.Sphere.ShrinkSeconds);
                if (s.DyingRemaining <= 0)
                {
                    _spheres.RemoveAt(i);
                }

                continue;
            }

            s.X += s.VelocityX * dtSeconds;
            s.Y += s.VelocityY * dtSeconds;

            if (s.X < s.Radius)
            {
                s.X = s.Radius;
                s.VelocityX = -s.VelocityX;
            }
            else if (s.X > width - s.Radius)
            {
                s.X = width - s.Radius;
                s.VelocityX = -s.VelocityX;
            }

            if (s.Y < s.Radius)
            {
                s.Y = s.Radius;
                s.VelocityY = -s.VelocityY;
            }
            else if (s.Y > height - s.Radius)
            {
                s.Y = height - s.Radius;
                s.VelocityY = -s.VelocityY;
            }
        }
    }

    private void ResolveProjectileHits()
    {
        for (var pi = _projectiles.Count - 1; pi >= 0; pi--)
        {
            var p = _projectiles[pi];
            for (var si = 0; si < _spheres.Count; si++)
            {
                var s = _spheres[si];
                if (s.IsDying)
                {
                    continue;
                }

                var dx = p.X - s.X;
                var dy = p.Y - s.Y;
                var reach = p.Radius + s.Radius;
                if ((dx * dx) + (dy * dy) <= reach * reach)
                {
                    _projectiles.RemoveAt(pi);
                    StartDying(s);
                    Score += _config.Scoring.PointsPerSphere;
                    SpheresDestroyedThisLevel++;
                    if (s.IsLifeSphere)
                    {
                        GrantLife();
                    }

                    break;
                }
            }
        }
    }

    private void ResolveShipCollisions()
    {
        var shipRadius = _config.Ship.Radius;
        foreach (var s in _spheres)
        {
            if (s.IsDying)
            {
                continue;
            }

            var dx = ShipX - s.X;
            var dy = ShipY - s.Y;
            var reach = shipRadius + s.Radius;
            if ((dx * dx) + (dy * dy) <= reach * reach)
            {
                Lives--;
                InvulnerabilityRemaining = _config.Ship.InvulnerabilitySeconds;
                StartDying(s);
                if (Lives <= 0)
                {
                    Lives = 0;
                    IsGameOver = true;
                }

                return;
            }
        }
    }

    private void CheckLevelUp()
    {
        if (SpheresDestroyedThisLevel < SpheresRequiredThisLevel)
        {
            return;
        }

        SpheresDestroyedThisLevel -= SpheresRequiredThisLevel;
        Level++;
        _spheres.Clear();
        _projectiles.Clear();
        _spawnAccumulator = 0;
        LevelIntroRemaining = _config.Levels.IntroSeconds;

        if (Level % _config.Lives.ExtraLifeEveryLevels == 0)
        {
            GrantLife();
        }
    }

    private void StartDying(Sphere sphere)
    {
        sphere.IsDying = true;
        sphere.DyingRemaining = _config.Sphere.ShrinkSeconds;
    }

    private void GrantLife() => Lives = Math.Min(Lives + 1, _config.Lives.MaxLives);
}
