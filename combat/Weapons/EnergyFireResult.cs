using psecsapi.Combat;
using psecsapi.Combat;

namespace psecsapi.Combat.Weapons;

/// <summary>
/// Result of an energy weapon firing attempt.
/// </summary>
public record EnergyFireResult(
    bool Hit,
    DamageResult? DamageResult,
    Vector2D FireDirection,
    double DistanceToTarget,
    double AttenuatedDamage);
