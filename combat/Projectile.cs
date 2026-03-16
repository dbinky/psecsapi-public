namespace psecsapi.Combat;

/// <summary>
/// Represents a kinetic projectile traveling across the combat grid.
/// Created when a kinetic weapon fires, then advanced each simulation tick
/// until it hits something or leaves the grid.
/// </summary>
public class Projectile
{
    public Guid Id { get; }
    public Vector2D Origin { get; }
    public Vector2D Position { get; private set; }
    public Vector2D Velocity { get; }
    public double Speed { get; }
    public DamageType DamageType { get; }
    public double Damage { get; }
    public Guid SourceShipId { get; }
    public Guid? TargetShipId { get; }
    public bool IsActive { get; private set; }

    public Projectile(
        Guid id,
        Vector2D origin,
        Vector2D velocity,
        double speed,
        DamageType damageType,
        double damage,
        Guid sourceShipId,
        Guid? targetShipId)
    {
        Id = id;
        Origin = origin;
        Position = origin;
        Velocity = velocity;
        Speed = speed;
        DamageType = damageType;
        Damage = damage;
        SourceShipId = sourceShipId;
        TargetShipId = targetShipId;
        IsActive = true;
    }

    /// <summary>
    /// Advances the projectile by one tick along its velocity vector.
    /// </summary>
    public void Advance()
    {
        if (!IsActive) return;
        Position = new Vector2D(Position.X + Velocity.X, Position.Y + Velocity.Y);
    }

    /// <summary>
    /// Returns true if the projectile has left the combat grid bounds.
    /// </summary>
    public bool IsOutOfBounds(CombatGrid grid)
    {
        return Position.X < grid.MinX || Position.X > grid.MaxX
            || Position.Y < grid.MinY || Position.Y > grid.MaxY;
    }

    /// <summary>
    /// Marks the projectile as inactive (hit something or left bounds).
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }
}
