using psecsapi.Domain.Combat;

namespace psecsapi.Combat;

/// <summary>
/// Calculates the weapon accuracy cone angle based on the weapon's sensitivity stat
/// and environmental modifiers (nebula, dense patches). Higher sensitivity = tighter cone = more accurate.
/// </summary>
public static class AccuracyConeCalculator
{
    /// <summary>
    /// Calculates the effective cone angle (in radians) for a weapon firing in the given conditions.
    /// Formula: cone = BaseConeAngle / (1 + sensitivity / SensitivityConeReduction)
    /// Nebula multiplier and dense patch multiplier stack multiplicatively.
    /// </summary>
    /// <param name="baseSensitivity">Weapon sensitivity stat (from module quality). 0 = worst accuracy.</param>
    /// <param name="isInNebula">True if combat is in a nebula sector (global penalty).</param>
    /// <param name="isInDensePatch">True if the firing ship is inside a nebula dense patch (additional penalty).</param>
    /// <returns>Cone half-angle in radians. Larger = less accurate.</returns>
    public static double CalculateConeAngle(double baseSensitivity, bool isInNebula, bool isInDensePatch)
    {
        // Base cone narrows with higher sensitivity
        double cone = CombatConstants.BaseConeAngle / (1.0 + baseSensitivity / CombatConstants.SensitivityConeReduction);

        // Nebula widens the cone (global sector modifier)
        if (isInNebula)
        {
            cone *= CombatConstants.NebulaConeMultiplier;
        }

        // Dense patch stacks on top of nebula penalty
        if (isInDensePatch)
        {
            cone *= CombatConstants.DensePatchConeMultiplier;
        }

        return cone;
    }

    /// <summary>
    /// Applies a random deviation within the accuracy cone to the aim direction.
    /// The deviation angle is uniformly distributed within [-coneAngle, +coneAngle].
    /// Returns a new unit vector representing the actual fire direction.
    /// </summary>
    /// <param name="aimDirection">Unit vector pointing from shooter toward target.</param>
    /// <param name="coneAngle">Half-angle of the accuracy cone in radians.</param>
    /// <param name="rng">Seeded RNG for deterministic replay.</param>
    /// <returns>New unit vector deviated within the cone.</returns>
    public static Vector2D ApplyConeDeviation(Vector2D aimDirection, double coneAngle, Random rng)
    {
        // Generate uniform random deviation within [-coneAngle, +coneAngle]
        double deviation = (rng.NextDouble() * 2.0 - 1.0) * coneAngle;

        // Rotate the aim direction by the deviation angle
        double cos = Math.Cos(deviation);
        double sin = Math.Sin(deviation);

        double newX = aimDirection.X * cos - aimDirection.Y * sin;
        double newY = aimDirection.X * sin + aimDirection.Y * cos;

        // Normalize to unit vector (should already be near-unit, but ensure precision)
        double length = Math.Sqrt(newX * newX + newY * newY);
        if (length < 1e-10) return aimDirection; // degenerate case

        return new Vector2D(newX / length, newY / length);
    }
}
