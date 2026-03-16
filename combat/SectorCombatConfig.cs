using psecsapi.Domain.Combat;

namespace psecsapi.Combat;

public static class SectorCombatConfig
{
    public record CombatTimingConfig(int TicksPerSecond, int MaxDurationSeconds, int MaxTotalTicks);

    public static CombatTimingConfig GetConfig(string sectorType)
    {
        return sectorType switch
        {
            "Void" => new CombatTimingConfig(
                CombatConstants.SimTicksPerSecond, 300, CombatConstants.SimTicksPerSecond * 300),
            "Rubble" => new CombatTimingConfig(
                CombatConstants.SimTicksPerSecond, 600, CombatConstants.SimTicksPerSecond * 600),
            "Nebula" => new CombatTimingConfig(
                CombatConstants.SimTicksPerSecond, 600, CombatConstants.SimTicksPerSecond * 600),
            "StarSystem" => new CombatTimingConfig(
                CombatConstants.SimTicksPerSecond, 600, CombatConstants.SimTicksPerSecond * 600),
            "BlackHole" => new CombatTimingConfig(
                CombatConstants.SimTicksPerSecond, 450, CombatConstants.SimTicksPerSecond * 450),
            "Nexus" => throw new InvalidOperationException(
                "Combat is prohibited in Nexus sectors"),
            _ => throw new ArgumentException(
                $"Unknown sector type: {sectorType}", nameof(sectorType))
        };
    }
}
