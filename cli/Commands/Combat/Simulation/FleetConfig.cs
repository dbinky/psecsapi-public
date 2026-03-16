namespace psecsapi.Console.Commands.Combat.Simulation;

/// <summary>
/// Configuration for a fleet preset used in local combat simulation.
/// Contains a list of ships with their loadouts, and optional script settings.
/// </summary>
public class FleetConfig
{
    public List<ShipConfig> Ships { get; set; } = new();
    public string? Script { get; set; }
    public string? ScriptFile { get; set; }
}
