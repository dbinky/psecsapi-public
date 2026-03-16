using psecsapi.Combat.Snapshots;
using psecsapi.Domain.Combat;
using psecsapi.Domain.Modules;

namespace psecsapi.Combat;

/// <summary>
/// Resolves weapon damage through the shield -> armor -> structure -> module hit pipeline.
/// Pure computation -- no side effects on the snapshot. Returns a DamageResult describing
/// what happened. Use DamageApplicator.ApplyDamageToShip to mutate the snapshot afterward.
/// </summary>
public static class DamagePipeline
{
    /// <summary>
    /// Resolves damage through the full pipeline: shields, armor, structure, module hit.
    /// Does NOT mutate the target snapshot -- call DamageApplicator.ApplyDamageToShip separately.
    /// </summary>
    public static DamageResult Resolve(double rawDamage, DamageType damageType, CombatShipSnapshot target, Random rng)
    {
        if (rawDamage <= 0)
        {
            return new DamageResult
            {
                RawDamage = rawDamage,
                Type = damageType,
                ShieldAbsorbed = 0,
                ArmorAblated = 0,
                StructureDamage = 0,
                ModuleHit = null,
                ModuleHitId = null,
                ModuleConditionDamage = 0,
                ShieldEffectiveness = 0,
                ArmorEffectiveness = 0,
                PowerDeliveryFactor = 1.0
            };
        }

        // Step 1: Calculate power delivery factor
        var powerDeliveryFactor = CalculatePowerDeliveryFactor(target);

        // Step 2: Shield reduction
        var shieldEffectiveness = CalculateShieldEffectiveness(target);
        var effectiveShield = shieldEffectiveness * Math.Min(1.0, powerDeliveryFactor);

        var shieldTypeMultiplier = damageType == DamageType.Energy
            ? CombatConstants.ShieldVsEnergyMultiplier
            : CombatConstants.ShieldVsKineticMultiplier;

        var absorbed = rawDamage * effectiveShield * shieldTypeMultiplier;
        absorbed = Math.Min(absorbed, rawDamage);

        // Step 3: Armor ablation
        var armorEffectiveness = CalculateArmorEffectiveness(target);

        var remaining = rawDamage - absorbed;

        var armorTypeMultiplier = damageType == DamageType.Kinetic
            ? CombatConstants.ArmorVsKineticMultiplier
            : CombatConstants.ArmorVsEnergyMultiplier;

        var ablated = remaining * armorEffectiveness * armorTypeMultiplier;
        ablated = Math.Min(ablated, remaining);

        // Step 4: Structure damage
        var structureDamage = rawDamage - absorbed - ablated;

        // Step 5: Module hit location
        var moduleHit = ResolveModuleHit(rawDamage, target, rng);

        return new DamageResult
        {
            RawDamage = rawDamage,
            Type = damageType,
            ShieldAbsorbed = absorbed,
            ArmorAblated = ablated,
            StructureDamage = structureDamage,
            ModuleHit = moduleHit,
            ModuleHitId = moduleHit?.ModuleId,
            ModuleConditionDamage = moduleHit?.ConditionDamage ?? 0,
            ShieldEffectiveness = effectiveShield,
            ArmorEffectiveness = armorEffectiveness,
            PowerDeliveryFactor = powerDeliveryFactor
        };
    }

    /// <summary>
    /// Calculates shield effectiveness as the condition-weighted ratio of EnergyResistance
    /// capabilities across all modules. Returns 0.0-1.0.
    /// Full condition modules contribute 1.0; a module at 50% condition contributes 0.5
    /// of its base resistance value to the total.
    /// </summary>
    public static double CalculateShieldEffectiveness(CombatShipSnapshot target)
    {
        var totalShield = 0.0;
        var maxShield = 0.0;

        foreach (var module in target.Modules)
        {
            foreach (var cap in module.Capabilities)
            {
                if (cap.CapabilityType == ModuleCapabilityType.EnergyResistance)
                {
                    maxShield += (double)cap.BaseValue;
                    totalShield += (double)cap.BaseValue * ((double)module.Condition / 100.0);
                }
            }
        }

        return maxShield > 0 ? totalShield / maxShield : 0.0;
    }

    /// <summary>
    /// Calculates armor effectiveness as the condition-weighted ratio of KineticResistance
    /// capabilities across all modules. Returns 0.0-1.0.
    /// Full condition modules contribute 1.0; a module at 50% condition contributes 0.5
    /// of its base resistance value to the total.
    /// </summary>
    public static double CalculateArmorEffectiveness(CombatShipSnapshot target)
    {
        var totalArmor = 0.0;
        var maxArmor = 0.0;

        foreach (var module in target.Modules)
        {
            foreach (var cap in module.Capabilities)
            {
                if (cap.CapabilityType == ModuleCapabilityType.KineticResistance)
                {
                    maxArmor += (double)cap.BaseValue;
                    totalArmor += (double)cap.BaseValue * ((double)module.Condition / 100.0);
                }
            }
        }

        return maxArmor > 0 ? totalArmor / maxArmor : 0.0;
    }

    /// <summary>
    /// Calculates the power delivery ratio: current total power generation (condition-weighted)
    /// divided by total power required by all modules. If power supply cannot meet demand,
    /// the ratio drops below 1.0, degrading all power-dependent capabilities (shields, weapons, etc.).
    /// Damaged modules still consume full power requirements.
    /// </summary>
    public static double CalculatePowerDeliveryFactor(CombatShipSnapshot target)
    {
        var currentPower = 0.0;
        var totalPowerRequired = 0.0;

        foreach (var module in target.Modules)
        {
            // Sum power generation (condition-weighted)
            foreach (var cap in module.Capabilities)
            {
                if (cap.CapabilityType == ModuleCapabilityType.PowerGeneration)
                {
                    currentPower += (double)cap.BaseValue * ((double)module.Condition / 100.0);
                }
            }

            // Sum power requirements (always full -- damaged modules still consume full power)
            totalPowerRequired += module.PowerRequired;
        }

        if (totalPowerRequired <= 0)
            return 1.0;

        return Math.Min(1.0, currentPower / totalPowerRequired);
    }

    /// <summary>
    /// Selects a random module to receive condition damage from the hit.
    /// Exterior modules are weighted 3x more likely than interior modules.
    /// Only modules with condition > 0 are eligible. Returns null if no modules are alive.
    /// </summary>
    private static ModuleHitResult? ResolveModuleHit(double rawDamage, CombatShipSnapshot target, Random rng)
    {
        // Build weighted candidate list -- only alive modules
        var candidates = new List<(ModuleSnapshot module, double weight)>();
        var totalWeight = 0.0;

        foreach (var module in target.Modules)
        {
            if (module.Condition <= 0)
                continue;

            var weight = module.IsExterior
                ? CombatConstants.ExteriorModuleHitWeight
                : CombatConstants.InteriorModuleHitWeight;

            candidates.Add((module, weight));
            totalWeight += weight;
        }

        if (candidates.Count == 0)
            return null;

        // Weighted random selection
        var roll = rng.NextDouble() * totalWeight;
        var cumulative = 0.0;
        ModuleSnapshot? selectedModule = null;

        foreach (var (module, weight) in candidates)
        {
            cumulative += weight;
            if (roll < cumulative)
            {
                selectedModule = module;
                break;
            }
        }

        // Fallback to last candidate (handles floating-point edge case where roll == totalWeight)
        selectedModule ??= candidates[^1].module;

        var conditionDamage = rawDamage * CombatConstants.ModuleHitDamagePercent;
        var conditionBefore = (double)selectedModule.Condition;
        var conditionAfter = Math.Max(0.0, conditionBefore - conditionDamage);

        return new ModuleHitResult
        {
            ModuleId = selectedModule.ModuleId,
            ModuleName = selectedModule.Name,
            ConditionBefore = conditionBefore,
            ConditionAfter = conditionAfter,
            ConditionDamage = conditionDamage,
            WasDestroyed = conditionAfter <= 0.0
        };
    }
}
