namespace Quadspace.Core.Engine;

/// <summary>A shot fired by the ship, travelling in one of the four axis directions.</summary>
public sealed class Projectile
{
    public double X { get; internal set; }
    public double Y { get; internal set; }
    public double VelocityX { get; internal set; }
    public double VelocityY { get; internal set; }
    public double Radius { get; internal set; }
}
