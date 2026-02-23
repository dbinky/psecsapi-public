using System.CommandLine;
using System.Text.Json;
using psecsapi.Console.Infrastructure.Http;

namespace psecsapi.Console.Commands.Ship;

public class ShipCatalogSubCommand
{
    private readonly AuthenticatedHttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ShipCatalogSubCommand(AuthenticatedHttpClient client)
    {
        _client = client;
    }

    public Command Build()
    {
        var classOption = new Option<string?>("--class", "Filter by ship class (e.g. Scout, Corvette)");
        var jsonOption = new Option<bool>("--json", "Output as raw JSON");

        var command = new Command("catalog", "List ship catalog configurations")
        {
            classOption,
            jsonOption
        };

        command.SetHandler(async (string? className, bool json) =>
        {
            var url = string.IsNullOrEmpty(className)
                ? "/api/ship-catalog"
                : $"/api/ship-catalog?class={className}";

            var response = await _client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                System.Console.WriteLine($"Error: {response.StatusCode}");
                System.Console.WriteLine(errorContent);
                return;
            }

            var content = await response.Content.ReadAsStringAsync();

            if (json)
            {
                System.Console.WriteLine(content);
                return;
            }

            var entries = JsonSerializer.Deserialize<List<CatalogEntry>>(content, JsonOptions);
            if (entries == null || entries.Count == 0)
            {
                System.Console.WriteLine("No ship configurations found.");
                return;
            }

            System.Console.WriteLine($"Ship Catalog ({entries.Count} configurations):");
            System.Console.WriteLine();

            foreach (var entry in entries)
            {
                System.Console.WriteLine($"  {entry.CatalogId} — {entry.Name}");
                System.Console.WriteLine($"    Class: {entry.Class}  Slots: {entry.InteriorSlots}i/{entry.ExteriorSlots}e ({entry.TotalSlots} total)");
                System.Console.WriteLine($"    Structure: {entry.BaseStructurePoints}  Hull: {entry.BaseHullPoints}  Mass: {entry.BaseMass}");
                System.Console.WriteLine();
            }
        }, classOption, jsonOption);

        return command;
    }

    private class CatalogEntry
    {
        public string CatalogId { get; set; } = "";
        public string Name { get; set; } = "";
        public string Class { get; set; } = "";
        public int InteriorSlots { get; set; }
        public int ExteriorSlots { get; set; }
        public int TotalSlots { get; set; }
        public decimal BaseStructurePoints { get; set; }
        public decimal BaseHullPoints { get; set; }
        public decimal BaseMass { get; set; }
    }
}
