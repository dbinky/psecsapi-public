using System.CommandLine;
using System.Text.Json;
using psecsapi.Console.Commands.Combat.Simulation;
using psecsapi.Console.Infrastructure.Configuration;
using psecsapi.Console.Infrastructure.Http;

namespace psecsapi.Console.Commands.Combat;

/// <summary>
/// Implements the 'papi combat export-fleet' command.
/// Exports a fleet's ships and loadouts from the API to a JSON file
/// compatible with the local combat simulate command.
/// </summary>
public class CombatExportFleetCommand
{
    private readonly AuthenticatedHttpClient _client;
    private readonly CliConfig _config;

    private static readonly JsonSerializerOptions JsonReadOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions JsonWriteOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public CombatExportFleetCommand(AuthenticatedHttpClient client, CliConfig config)
    {
        _client = client;
        _config = config;
    }

    public Command Build()
    {
        var fleetIdArg = new Argument<Guid>("fleet-id", "Fleet ID to export");
        var outputOption = new Option<string?>("--output", "Output file path (default: fleet-{id}.json)");
        outputOption.AddAlias("-o");

        var cmd = new Command("export-fleet", "Export a fleet's loadout to a JSON file for local simulation")
        {
            fleetIdArg, outputOption
        };

        cmd.SetHandler(async (Guid fleetId, string? output) =>
        {
            try
            {
                await ExportFleet(fleetId, output);
            }
            catch (Exception ex)
            {
                System.Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.ExitCode = 1;
            }
        }, fleetIdArg, outputOption);

        return cmd;
    }

    private async Task ExportFleet(Guid fleetId, string? output)
    {
        // Fetch fleet details
        var fleetResponse = await _client.GetAsync($"/api/fleet/{fleetId}");
        if (!fleetResponse.IsSuccessStatusCode)
        {
            var errorContent = await fleetResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Failed to fetch fleet {fleetId}: {fleetResponse.StatusCode} — {errorContent}");
        }

        var fleetJson = await fleetResponse.Content.ReadAsStringAsync();
        var fleet = JsonSerializer.Deserialize<FleetResponse>(fleetJson, JsonReadOptions);

        if (fleet == null)
            throw new InvalidOperationException("Failed to parse fleet response.");

        // Fetch each ship's details
        var ships = new List<ShipConfig>();
        foreach (var shipId in fleet.ShipIds)
        {
            var shipResponse = await _client.GetAsync($"/api/ship/{shipId}");
            if (!shipResponse.IsSuccessStatusCode)
            {
                System.Console.Error.WriteLine($"Warning: Failed to fetch ship {shipId}, skipping.");
                continue;
            }

            var shipJson = await shipResponse.Content.ReadAsStringAsync();
            var ship = JsonSerializer.Deserialize<ShipResponse>(shipJson, JsonReadOptions);

            if (ship == null)
            {
                System.Console.Error.WriteLine($"Warning: Failed to parse ship {shipId}, skipping.");
                continue;
            }

            var shipConfig = new ShipConfig
            {
                Name = ship.Name,
                Chassis = ship.Chassis,
                Weapons = ship.Weapons?.Select(w => w.Name).ToList() ?? new List<string>(),
                Modules = ship.Modules?.Select(m => new ModuleConfig
                {
                    Type = m.Name,
                    Slot = m.IsExterior ? "Exterior" : "Interior"
                }).ToList() ?? new List<ModuleConfig>(),
                Cargo = ship.CargoUsed
            };

            ships.Add(shipConfig);
        }

        if (ships.Count == 0)
            throw new InvalidOperationException("No ships could be exported from the fleet.");

        var fleetConfig = new FleetConfig
        {
            Ships = ships,
            Script = null,
            ScriptFile = null
        };

        // Write to file
        var filePath = Path.GetFullPath(output ?? $"fleet-{fleetId}.json");
        var configJson = JsonSerializer.Serialize(fleetConfig, JsonWriteOptions);
        await File.WriteAllTextAsync(filePath, configJson);

        System.Console.WriteLine($"Fleet exported: {filePath} ({ships.Count} ships)");
    }

    // ── Response models ────────────────────────────────────────────

    private class FleetResponse
    {
        public Guid FleetId { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<Guid> ShipIds { get; set; } = new();
    }

    private class ShipResponse
    {
        public Guid ShipId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Chassis { get; set; } = string.Empty;
        public double CargoUsed { get; set; }
        public List<ShipModuleResponse>? Weapons { get; set; }
        public List<ShipModuleResponse>? Modules { get; set; }
    }

    private class ShipModuleResponse
    {
        public string Name { get; set; } = string.Empty;
        public bool IsExterior { get; set; }
    }
}
