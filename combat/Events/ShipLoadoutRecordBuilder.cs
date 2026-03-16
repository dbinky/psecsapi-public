using psecsapi.Combat.Snapshots;

namespace psecsapi.Combat.Events;

/// <summary>
/// Builds ShipLoadoutRecord entries for the replay CombatStartedEvent.
/// Shared between CombatInstanceRoot (production) and CombatSimulateCommand (CLI).
/// </summary>
public static class ShipLoadoutRecordBuilder
{
    /// <summary>
    /// Builds ShipLoadoutRecords for both fleets (attackers = side 0, defenders = side 1).
    /// </summary>
    public static List<ShipLoadoutRecord> Build(
        List<CombatShipSnapshot> attackers, List<CombatShipSnapshot> defenders)
    {
        var records = new List<ShipLoadoutRecord>();
        AddRecords(records, attackers, fleetSide: 0);
        AddRecords(records, defenders, fleetSide: 1);
        return records;
    }

    private static void AddRecords(
        List<ShipLoadoutRecord> records, List<CombatShipSnapshot> ships, int fleetSide)
    {
        foreach (var ship in ships)
        {
            var moduleRecords = new List<ModuleLoadoutRecord>();
            foreach (var module in ship.Modules)
            {
                // Use the first capability type as the primary capability for categorization
                var primaryCapability = module.Capabilities.FirstOrDefault()?.CapabilityType.ToString() ?? "Unknown";
                moduleRecords.Add(new ModuleLoadoutRecord
                {
                    ModuleId = module.ModuleId.ToString(),
                    Name = module.Name,
                    Capability = primaryCapability,
                    Condition = (double)module.Condition
                });
            }

            records.Add(new ShipLoadoutRecord
            {
                ShipId = ship.ShipId.ToString(),
                FleetSide = fleetSide,
                StartX = ship.Position.X,
                StartY = ship.Position.Y,
                MaxSpeed = ship.MaxSpeed,
                MaxAcceleration = ship.MaxAcceleration,
                StructurePoints = (double)ship.MaxStructurePoints,
                ShieldEffectiveness = ship.ShieldEffectiveness,
                ArmorEffectiveness = ship.ArmorEffectiveness,
                Mass = ship.Mass,
                ComputeCapacity = ship.ComputeCapacity,
                Modules = moduleRecords
            });
        }
    }
}
