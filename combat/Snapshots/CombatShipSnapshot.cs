using Orleans;
using psecsapi.Grains.Interfaces.BoxedAsset.Models;

namespace psecsapi.Combat.Snapshots;

/// <summary>
/// Complete frozen snapshot of a ship's combat state.
/// Created once at combat start from grain data; the simulation operates entirely on these snapshots.
/// Mutable fields (position, velocity, structure points, module conditions) are updated by the simulation engine.
/// </summary>
[GenerateSerializer]
public class CombatShipSnapshot
{
    // === Identity ===

    /// <summary>Ship grain ID.</summary>
    [Id(0)] public Guid ShipId { get; init; }

    /// <summary>Ship display name.</summary>
    [Id(1)] public string ShipName { get; init; } = string.Empty;

    /// <summary>Owning corporation ID.</summary>
    [Id(2)] public Guid CorpId { get; init; }

    /// <summary>Fleet this ship belongs to.</summary>
    [Id(3)] public Guid FleetId { get; init; }

    // === Position & Physics (mutable during simulation) ===

    /// <summary>Current position on the combat grid.</summary>
    [Id(4)] public Vector2D Position { get; set; }

    /// <summary>Current velocity vector (units per tick).</summary>
    [Id(5)] public Vector2D Velocity { get; set; }

    /// <summary>Current acceleration vector (units per tick^2). Set by thrust commands during simulation.</summary>
    [Id(26)] public Vector2D Acceleration { get; set; } = Vector2D.Zero;

    /// <summary>Current facing direction in radians.</summary>
    [Id(6)] public double Facing { get; set; }

    /// <summary>Current scalar speed (magnitude of velocity).</summary>
    [Id(7)] public double CurrentSpeed { get; set; }

    // === Stats (derived from modules at snapshot time) ===

    /// <summary>Maximum speed from engine module Speed capabilities.</summary>
    [Id(8)] public double MaxSpeed { get; init; }

    /// <summary>Maximum acceleration (thrust-to-mass ratio). Speed / Mass.</summary>
    [Id(9)] public double MaxAcceleration { get; init; }

    /// <summary>Total ship mass (chassis + hull + modules).</summary>
    [Id(10)] public double Mass { get; init; }

    /// <summary>Total compute capacity from processing modules.</summary>
    [Id(11)] public double ComputeCapacity { get; init; }

    // === Durability (mutable during simulation) ===

    /// <summary>Current structure points. Ship destroyed when this reaches 0.</summary>
    [Id(12)] public decimal CurrentStructurePoints { get; set; }

    /// <summary>Maximum structure points from chassis.</summary>
    [Id(13)] public decimal MaxStructurePoints { get; init; }

    /// <summary>Current hull points.</summary>
    [Id(14)] public decimal CurrentHullPoints { get; set; }

    /// <summary>Maximum hull points from hull material.</summary>
    [Id(15)] public decimal MaxHullPoints { get; init; }

    // === Weapons ===

    /// <summary>All weapon modules on this ship, as frozen WeaponSnapshots.</summary>
    [Id(16)] public List<WeaponSnapshot> Weapons { get; init; } = new();

    // === All Modules ===

    /// <summary>All modules on this ship (including weapons, shields, engines, etc.).</summary>
    [Id(17)] public List<ModuleSnapshot> Modules { get; init; } = new();

    // === Defense ===

    /// <summary>Total energy resistance from shield modules.</summary>
    [Id(18)] public double TotalEnergyResistance { get; init; }

    /// <summary>Total kinetic resistance from armor modules.</summary>
    [Id(19)] public double TotalKineticResistance { get; init; }

    /// <summary>Total power generation from reactor modules.</summary>
    [Id(20)] public double TotalPowerGeneration { get; init; }

    /// <summary>Total power required by all modules (sum of Power requirements).</summary>
    [Id(25)] public double TotalPowerRequired { get; init; }

    // === Sensors ===

    /// <summary>Sensor capability value, affects starting distance and detection.</summary>
    [Id(21)] public double SensorCapability { get; init; }

    // === Cargo ===

    /// <summary>Cargo manifest for loot drop calculation on destruction. (assetId, assetType) pairs.</summary>
    [Id(22)] public List<CargoEntry> Cargo { get; init; } = new();

    // === Status (mutable during simulation) ===

    /// <summary>True while the ship has structure points remaining.</summary>
    [Id(23)] public bool IsAlive { get; set; } = true;

    /// <summary>True if the ship has fled the combat grid.</summary>
    [Id(24)] public bool HasFled { get; set; } = false;

    // === Computed Properties ===

    /// <summary>
    /// Shield effectiveness (0.0-1.0). Delegates to DamagePipeline.CalculateShieldEffectiveness
    /// to ensure consistency between display/scripting and actual damage resolution.
    /// </summary>
    public double ShieldEffectiveness => DamagePipeline.CalculateShieldEffectiveness(this);

    /// <summary>
    /// Armor effectiveness (0.0-1.0). Delegates to DamagePipeline.CalculateArmorEffectiveness
    /// to ensure consistency between display/scripting and actual damage resolution.
    /// </summary>
    public double ArmorEffectiveness => DamagePipeline.CalculateArmorEffectiveness(this);
}
