using System.CommandLine;
using System.Text.Json;
using psecsapi.Console.Infrastructure.Configuration;
using psecsapi.Console.Infrastructure.Http;

namespace psecsapi.Console.Commands.Loot
{
    public class LootCommand : ICommand
    {
        private readonly AuthenticatedHttpClient _client;
        private readonly CliConfig _config;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public LootCommand(AuthenticatedHttpClient client, CliConfig config)
        {
            _client = client;
            _config = config;
        }

        public Command Build()
        {
            var command = new Command("loot", "Loot field scanning and pickup after combat");

            command.AddCommand(BuildScanCommand());
            command.AddCommand(BuildPickupCommand());

            return command;
        }

        // ── loot scan ──────────────────────────────────────────────────

        private Command BuildScanCommand()
        {
            var sectorIdArg = new Argument<Guid>("sector-id", "Sector ID to scan for loot fields");
            var jsonOption = new Option<bool>("--json", "Output as raw JSON");

            var cmd = new Command("scan", "Scan a sector for loot fields dropped by destroyed ships") { sectorIdArg, jsonOption };
            cmd.SetHandler(async (Guid sectorId, bool json) =>
            {
                var response = await _client.GetAsync($"/api/sector/{sectorId}/loot");
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    System.Console.WriteLine($"Error: {response.StatusCode}");
                    System.Console.WriteLine(content);
                    return;
                }

                if (json)
                {
                    System.Console.WriteLine(content);
                    return;
                }

                var lootFields = JsonSerializer.Deserialize<List<LootField>>(content, JsonOptions);
                if (lootFields == null)
                {
                    System.Console.WriteLine("Error: Unable to parse response.");
                    return;
                }

                System.Console.WriteLine();
                if (lootFields.Count == 0)
                {
                    System.Console.WriteLine("No loot fields in this sector.");
                    System.Console.WriteLine();
                    return;
                }

                System.Console.WriteLine($"Loot Fields ({lootFields.Count}):");
                System.Console.WriteLine();
                System.Console.WriteLine($"  {"ID",-38} {"Items",5} {"Access",-12} {"Expires",-20}");
                System.Console.WriteLine($"  {new string('-', 78)}");

                foreach (var field in lootFields)
                {
                    var access = field.IsExclusive ? "EXCLUSIVE" : "Public";
                    var expires = field.ExpiresAt.ToString("yyyy-MM-dd HH:mm:ss") + " UTC";
                    System.Console.WriteLine($"  {field.Id,-38} {field.ItemCount,5} {access,-12} {expires,-20}");
                }

                System.Console.WriteLine();
                System.Console.WriteLine("Pick up with: papi loot pickup <sector-id> <loot-id> --fleet <fleet-id> --ship <ship-id>");
                System.Console.WriteLine();
            }, sectorIdArg, jsonOption);

            return cmd;
        }

        // ── loot pickup ────────────────────────────────────────────────

        private Command BuildPickupCommand()
        {
            var sectorIdArg = new Argument<Guid>("sector-id", "Sector ID containing the loot field");
            var lootIdArg = new Argument<Guid>("loot-id", "Loot field ID to pick up from");
            var fleetOption = new Option<Guid>("--fleet", "Fleet ID (must be in the sector)") { IsRequired = true };
            fleetOption.AddAlias("-f");
            var shipOption = new Option<Guid>("--ship", "Ship ID to receive the cargo") { IsRequired = true };
            shipOption.AddAlias("-s");
            var jsonOption = new Option<bool>("--json", "Output as raw JSON");

            var cmd = new Command("pickup", "Pick up items from a loot field into a ship's cargo") { sectorIdArg, lootIdArg, fleetOption, shipOption, jsonOption };
            cmd.SetHandler(async (Guid sectorId, Guid lootId, Guid fleetId, Guid shipId, bool json) =>
            {
                var response = await _client.PostAsync($"/api/sector/{sectorId}/loot/{lootId}/pickup", new
                {
                    FleetId = fleetId,
                    ShipId = shipId
                });
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    System.Console.WriteLine($"Error: {response.StatusCode}");
                    System.Console.WriteLine(content);
                    return;
                }

                if (json)
                {
                    System.Console.WriteLine(content);
                    return;
                }

                // API returns List<CombatLootItem> — a JSON array of { assetId, type } entries
                var items = JsonSerializer.Deserialize<List<LootPickupItem>>(content, JsonOptions);

                System.Console.WriteLine("Loot collected!");
                System.Console.WriteLine();

                if (items != null && items.Count > 0)
                {
                    System.Console.WriteLine($"  {"Type",-15} {"Asset ID",-38}");
                    System.Console.WriteLine($"  {new string('-', 55)}");
                    foreach (var item in items)
                        System.Console.WriteLine($"  {item.Type,-15} {item.AssetId}");
                    System.Console.WriteLine();
                }
                else
                {
                    System.Console.WriteLine("  No items collected (loot field may have been empty).");
                    System.Console.WriteLine();
                }

                System.Console.WriteLine($"Check cargo with: papi ship cargo {shipId}");
            }, sectorIdArg, lootIdArg, fleetOption, shipOption, jsonOption);

            return cmd;
        }

        // ── Response models ────────────────────────────────────────────

        private class LootField
        {
            public Guid Id { get; set; }
            public double PositionX { get; set; }
            public double PositionY { get; set; }
            public int ItemCount { get; set; }
            public bool IsExclusive { get; set; }
            public DateTime ExpiresAt { get; set; }
        }

        private class LootPickupItem
        {
            public Guid AssetId { get; set; }
            public string Type { get; set; } = string.Empty;
        }
    }
}
