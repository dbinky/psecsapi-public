namespace psecsapi.Console.Commands.Combat.Simulation;

/// <summary>
/// Configuration for a single module on a ship in a fleet preset.
/// </summary>
public class ModuleConfig
{
    public string Type { get; set; } = string.Empty;
    public string Slot { get; set; } = "Interior";
}
