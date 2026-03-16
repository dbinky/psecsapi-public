namespace psecsapi.Console.Commands.Combat.Simulation;

/// <summary>
/// Configuration for a single ship in a fleet preset.
/// Maps to a CombatShipSnapshot for local simulation.
/// </summary>
public class ShipConfig
{
    public string Name { get; set; } = string.Empty;
    public string Chassis { get; set; } = string.Empty;
    public List<string> Weapons { get; set; } = new();
    public List<ModuleConfig> Modules { get; set; } = new();
    public double Cargo { get; set; }
}
