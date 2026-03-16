using psecsapi.Combat.Snapshots;
using psecsapi.Domain.Modules;

namespace psecsapi.Combat;

/// <summary>
/// Applies a DamageResult to a CombatShipSnapshot, mutating its state.
/// Handles structure point reduction, module condition damage, ship destruction,
/// and post-damage recalculation of derived combat stats.
/// </summary>
public static class DamageApplicator
{
    /// <summary>
    /// Applies the given damage result to the ship snapshot. Mutates the snapshot in place.
    /// After applying:
    ///   - CurrentStructurePoints is reduced by StructureDamage
    ///   - The hit module's condition is reduced
    ///   - If structure reaches 0, IsAlive is set to false
    /// </summary>
    public static void ApplyDamageToShip(CombatShipSnapshot ship, DamageResult result)
    {
        // Apply structure damage
        ship.CurrentStructurePoints = Math.Max(0m, ship.CurrentStructurePoints - (decimal)result.StructureDamage);

        // Apply module condition damage
        if (result.ModuleHit != null)
        {
            var hitModule = ship.Modules.FirstOrDefault(m => m.ModuleId == result.ModuleHit.ModuleId);
            if (hitModule != null)
            {
                // ModuleSnapshot uses record class with init properties, but Condition
                // is needed as mutable during simulation. We update via the list.
                var index = ship.Modules.IndexOf(hitModule);
                ship.Modules[index] = hitModule with { Condition = (decimal)result.ModuleHit.ConditionAfter };
            }
        }

        // Check for ship destruction
        if (ship.CurrentStructurePoints <= 0)
        {
            ship.IsAlive = false;
        }
    }

    /// <summary>
    /// Calculates current effective speed from engine modules (condition-weighted).
    /// Falls back to ship.MaxSpeed when no Speed-capable modules are present,
    /// so ships with no module list (e.g., test fixtures) behave correctly.
    /// </summary>
    public static double CalculateEffectiveSpeed(CombatShipSnapshot ship)
        => CalculateEffectiveCapability(ship, ModuleCapabilityType.Speed, ship.MaxSpeed);

    /// <summary>
    /// Calculates current effective acceleration (condition-weighted).
    /// Falls back to ship.MaxAcceleration when no Speed-capable modules are present.
    /// Uses the same formula as ShipSnapshotFactory: (speed / 100) * (1000 / (mass + 1000))
    /// to produce acceleration values consistent with the snapshot's MaxAcceleration.
    /// Damaged engines reduce effective speed, which proportionally reduces acceleration.
    /// </summary>
    public static double CalculateEffectiveAcceleration(CombatShipSnapshot ship)
    {
        var (hasModules, effectiveSpeed) = CalculateEffectiveCapabilityRaw(ship, ModuleCapabilityType.Speed);

        if (!hasModules)
            return ship.MaxAcceleration;

        // Match ShipSnapshotFactory formula: (speed / 100) * massFactor
        // where massFactor = 1000 / (mass + 1000)
        double massFactor = 1000.0 / (ship.Mass + 1000.0);
        return (effectiveSpeed / 100.0) * massFactor;
    }

    /// <summary>
    /// Calculates current effective compute capacity (condition-weighted).
    /// Falls back to ship.ComputeCapacity when no ComputeCapacity modules are present.
    /// </summary>
    public static double CalculateEffectiveCompute(CombatShipSnapshot ship)
        => CalculateEffectiveCapability(ship, ModuleCapabilityType.ComputeCapacity, ship.ComputeCapacity);

    /// <summary>
    /// Calculates current effective sensor capability (condition-weighted).
    /// Falls back to ship.SensorCapability when no Sensors modules are present.
    /// </summary>
    public static double CalculateEffectiveSensors(CombatShipSnapshot ship)
        => CalculateEffectiveCapability(ship, ModuleCapabilityType.Sensors, ship.SensorCapability);

    /// <summary>
    /// Shared helper: sums condition-weighted baseValues for the given capability type.
    /// Returns <paramref name="fallback"/> when no modules with the capability are present.
    /// </summary>
    private static double CalculateEffectiveCapability(
        CombatShipSnapshot ship, ModuleCapabilityType capType, double fallback)
    {
        var (hasModules, total) = CalculateEffectiveCapabilityRaw(ship, capType);
        return hasModules ? total : fallback;
    }

    /// <summary>
    /// Core loop: iterates modules and sums condition-weighted baseValues for <paramref name="capType"/>.
    /// Returns whether any matching modules were found and the raw total.
    /// </summary>
    private static (bool HasModules, double Total) CalculateEffectiveCapabilityRaw(
        CombatShipSnapshot ship, ModuleCapabilityType capType)
    {
        var total = 0.0;
        var hasModules = false;
        foreach (var module in ship.Modules)
        {
            foreach (var cap in module.Capabilities)
            {
                if (cap.CapabilityType == capType)
                {
                    hasModules = true;
                    total += (double)cap.BaseValue * ((double)module.Condition / 100.0);
                }
            }
        }
        return (hasModules, total);
    }
}
