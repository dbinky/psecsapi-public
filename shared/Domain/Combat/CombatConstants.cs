namespace psecsapi.Domain.Combat;

/// <summary>
/// All combat balance constants. Single source of truth for tuning.
/// </summary>
public static class CombatConstants
{
    // === Grid ===
    public const double GridSize = 20000.0;
    public const double GridMin = -10000.0;
    public const double GridMax = 10000.0;

    // === Starting Positions ===
    public const double BaseStartDistance = 6000.0;
    public const double SensorDistanceMultiplier = 50.0;
    public const double FleetSpreadPerShip = 200.0;

    // === Damage Type Multipliers (Rock-Paper-Scissors) ===
    public const double ShieldVsEnergyMultiplier = 0.60;
    public const double ShieldVsKineticMultiplier = 0.25;
    public const double ArmorVsKineticMultiplier = 0.60;
    public const double ArmorVsEnergyMultiplier = 0.25;

    // === Module Hit ===
    public const double ModuleHitDamagePercent = 0.15;
    public const double ExteriorModuleHitWeight = 3.0;
    public const double InteriorModuleHitWeight = 1.0;

    // === Energy Weapon Attenuation ===
    public const double EnergyFullDamageRangePercent = 0.25;
    public const double EnergyMinDamagePercent = 0.25;

    // === Accuracy Cone ===
    public const double BaseConeAngle = 0.10;
    public const double SensitivityConeReduction = 50.0;
    public const double NebulaConeMultiplier = 1.5;
    public const double DensePatchConeMultiplier = 2.0;

    // === Projectile ===
    public const double BaseProjectileSpeed = 200.0;

    // === Obstacles ===
    public const double SmallObstacleMaxRadius = 100.0;

    // === Environment ===
    public const double StarHeatDamageRadiusMultiplier = 1.5;
    public const double StarHeatDamagePerTick = 5.0;
    public const double BlackHoleGravityMaxRange = 8000.0;
    public const double BlackHoleBaseAcceleration = 2.0;
    public const double CollisionDamageMultiplier = 0.5;

    // === Compute Ticks ===
    public const int BaseComputeInterval = 10;
    public const double ComputeCapacityBase = 10.0;

    // === Simulation Tick Rate ===
    public const int SimTicksPerSecond = 10;

    // === Script Limits ===
    public const int MaxJintStepsPerTick = 10000;
    public const int MaxScriptSizePerCompute = 16384;
    public const int MaxScriptsPerCorp = 20;
    public const int MaxScriptNameLength = 100;
    public const int MaxScriptSourceBytes = 5 * 1024 * 1024;  // 5MB practical limit

    // === Loot ===
    public const double CargoDropPercent = 0.50;
    public const int LootExclusivityHours = 1;
    public const int LootDespawnHours = 24;

    // === Weapon Range ===
    /// <summary>
    /// Base weapon range for ships with no WeaponRange modules (TargetingArray etc.).
    /// Expressed in grid units (grid is 20,000 x 20,000, starting separation ~6,000).
    /// Ships always have some minimum firing range even without targeting upgrades.
    /// </summary>
    public const double BaseWeaponRange = 3000.0;

    // === Cooldown ===
    public const int CombatCooldownMinutes = 60;

    // === Retention ===
    public const int ReplayRetentionDays = 90;
}
