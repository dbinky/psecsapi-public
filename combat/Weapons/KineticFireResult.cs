using psecsapi.Combat;
using psecsapi.Combat;

namespace psecsapi.Combat.Weapons;

/// <summary>
/// Result of a kinetic weapon firing attempt.
/// </summary>
public record KineticFireResult(
    bool Created,
    Projectile? Projectile,
    Vector2D FireDirection);
