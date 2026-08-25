namespace Quadspace.Core.Engine;

/// <summary>A metallic sphere: moves at constant velocity, bounces off walls, dies when shot.</summary>
public sealed class Sphere
{
    public double X { get; internal set; }
    public double Y { get; internal set; }
    public double VelocityX { get; internal set; }
    public double VelocityY { get; internal set; }
    public double Radius { get; internal set; }

    /// <summary>A rare, specially-marked sphere that grants +1 life when destroyed by a shot.</summary>
    public bool IsLifeSphere { get; internal set; }

    /// <summary>True once hit; the sphere is shrinking out and no longer collides.</summary>
    public bool IsDying { get; internal set; }

    /// <summary>Seconds of shrink animation remaining.</summary>
    public double DyingRemaining { get; internal set; }

    /// <summary>Current size fraction for rendering (1 = full, 0 = gone).</summary>
    public double ShrinkFraction { get; internal set; } = 1.0;
}
