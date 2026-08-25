using Quadspace.Core.Configuration;

namespace Quadspace.Core.Engine;

/// <summary>
/// Deterministic, side-effect-free game simulation. Owns the authoritative game state and advances it
/// one frame at a time via <see cref="Update"/>. Rendering, input, and timing live outside (JS interop).
/// </summary>
public sealed class GameEngine
{
    private readonly GameConfig _config;

    public GameEngine(GameConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        _config = config;
        ShipX = config.Arena.Width / 2;
        ShipY = config.Arena.Height / 2;
        Lives = config.Ship.StartLives;
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

    /// <summary>
    /// Advances the simulation by <paramref name="dtSeconds"/>. <paramref name="moveX"/> and
    /// <paramref name="moveY"/> are an input direction in [-1, 1]; a diagonal is normalized so the ship
    /// never moves faster than <c>ship.speed</c>. The ship is clamped within the arena bounds.
    /// </summary>
    public void Update(double dtSeconds, double moveX, double moveY)
    {
        if (dtSeconds <= 0)
        {
            return;
        }

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
}
