using psecsapi.Combat;
using psecsapi.Combat;
using psecsapi.Combat.Snapshots;

namespace psecsapi.Combat.Weapons;

/// <summary>
/// Result of a projectile collision check.
/// </summary>
public record ProjectileCollisionResult(
    CombatShipSnapshot? HitShip,
    bool DestroyedByObstacle,
    CombatGridObstacle? HitObstacle);
