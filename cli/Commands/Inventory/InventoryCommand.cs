using System.CommandLine;
using System.Text.Json;
using psecsapi.Console.Infrastructure.Configuration;
using psecsapi.Console.Infrastructure.Http;

namespace psecsapi.Console.Commands.Inventory
{
    public class InventoryCommand : ICommand
    {
        private readonly AuthenticatedHttpClient _client;
        private readonly CliConfig _config;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public InventoryCommand(AuthenticatedHttpClient client, CliConfig config)
        {
            _client = client;
            _config = config;
        }

        public Command Build()
        {
            var command = new Command("inventory", "View owned resources across ships and fleets");

            command.AddCommand(BuildCorpCommand());
            command.AddCommand(BuildFleetCommand());
            command.AddCommand(BuildShipCommand());

            return command;
        }

        #region Corp Command

        private Command BuildCorpCommand()
        {
            var jsonOption = new Option<bool>("--json", "Output as raw JSON");

            var command = new Command("corp", "View corp-wide inventory with fleet breakdown");
            command.AddOption(jsonOption);

            command.SetHandler(async (bool json) =>
            {
                var corpId = _config.User.DefaultCorpId;
                if (!corpId.HasValue)
                {
                    System.Console.WriteLine("Error: No default corporation set. Use 'config set-corp <corp-id>' first.");
                    return;
                }

                var response = await _client.GetAsync($"/api/corp/{corpId.Value}/inventory");
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    System.Console.WriteLine(content);
                    return;
                }

                if (json)
                {
                    System.Console.WriteLine(content);
                    return;
                }

                var inventory = JsonSerializer.Deserialize<CorpInventoryResponse>(content, JsonOptions);

                if (inventory == null)
                {
                    System.Console.WriteLine("Error: Unable to parse response.");
                    return;
                }

                if (inventory.Totals.Count == 0)
                {
                    System.Console.WriteLine("No resources in inventory.");
                    System.Console.WriteLine("Extract resources from sectors to populate your inventory.");
                    return;
                }

                System.Console.WriteLine();
                System.Console.WriteLine("=== Corp Inventory ===");
                System.Console.WriteLine($"Snapshot: {inventory.SnapshotTime:yyyy-MM-dd HH:mm:ss} UTC");
                System.Console.WriteLine();

                // Group by resource class
                var byClass = inventory.Totals.GroupBy(t => t.ResourceClass);

                foreach (var classGroup in byClass.OrderBy(g => g.Key))
                {
                    System.Console.WriteLine($"--- {classGroup.Key} ---");

                    foreach (var resource in classGroup.OrderByDescending(r => r.TotalQuantity))
                    {
                        System.Console.WriteLine($"  {resource.ResourceName,-25} {FormatQuantity(resource.TotalQuantity),15}");

                        if (resource.ByFleet.Count > 0)
                        {
                            foreach (var fleet in resource.ByFleet.OrderByDescending(f => f.Quantity))
                            {
                                System.Console.WriteLine($"    {fleet.FleetName,-23} {FormatQuantity(fleet.Quantity),15}");
                            }
                        }
                    }
                    System.Console.WriteLine();
                }

                // Fleet summary
                if (inventory.Fleets.Count > 0)
                {
                    System.Console.WriteLine("=== Fleet Summary ===");
                    foreach (var fleet in inventory.Fleets.OrderByDescending(f => f.TotalQuantity))
                    {
                        System.Console.WriteLine($"  {fleet.FleetName,-25} {FormatQuantity(fleet.TotalQuantity),15} ({fleet.ResourceTypeCount} types)");
                    }
                }
            }, jsonOption);

            return command;
        }

        #endregion

        #region Fleet Command

        private Command BuildFleetCommand()
        {
            var fleetIdArg = new Argument<string>("fleet-id", "The fleet ID to view inventory for");
            var jsonOption = new Option<bool>("--json", "Output as raw JSON");

            var command = new Command("fleet", "View fleet inventory with ship breakdown") { fleetIdArg };
            command.AddOption(jsonOption);

            command.SetHandler(async (string fleetIdStr, bool json) =>
            {
                var corpId = _config.User.DefaultCorpId;
                if (!corpId.HasValue)
                {
                    System.Console.WriteLine("Error: No default corporation set. Use 'config set-corp <corp-id>' first.");
                    return;
                }

                if (!Guid.TryParse(fleetIdStr, out var fleetId))
                {
                    System.Console.WriteLine("Error: Invalid fleet ID format.");
                    return;
                }

                var response = await _client.GetAsync($"/api/corp/{corpId.Value}/inventory/fleet/{fleetId}");
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    System.Console.WriteLine(content);
                    return;
                }

                if (json)
                {
                    System.Console.WriteLine(content);
                    return;
                }

                var inventory = JsonSerializer.Deserialize<FleetInventoryResponse>(content, JsonOptions);

                if (inventory == null)
                {
                    System.Console.WriteLine("Error: Unable to parse response.");
                    return;
                }

                System.Console.WriteLine();
                System.Console.WriteLine($"=== Fleet: {inventory.FleetName} ===");
                System.Console.WriteLine($"Snapshot: {inventory.SnapshotTime:yyyy-MM-dd HH:mm:ss} UTC");
                System.Console.WriteLine();

                if (inventory.Totals.Count == 0)
                {
                    System.Console.WriteLine("No resources in this fleet.");
                    return;
                }

                // Resource totals
                System.Console.WriteLine("--- Resources ---");
                foreach (var resource in inventory.Totals.OrderByDescending(r => r.TotalQuantity))
                {
                    System.Console.WriteLine($"  {resource.ResourceName,-25} {FormatQuantity(resource.TotalQuantity),15}");
                }
                System.Console.WriteLine();

                // Ship breakdown
                if (inventory.Ships.Count > 0)
                {
                    System.Console.WriteLine("--- Ships ---");
                    foreach (var ship in inventory.Ships.OrderByDescending(s => s.TotalQuantity))
                    {
                        var extracting = ship.HasActiveExtraction ? " [Extracting]" : "";
                        System.Console.WriteLine($"  {ship.ShipName,-25} {FormatQuantity(ship.TotalQuantity),15} ({ship.ResourceTypeCount} types){extracting}");
                    }
                }
            }, fleetIdArg, jsonOption);

            return command;
        }

        #endregion

        #region Ship Command

        private Command BuildShipCommand()
        {
            var shipIdArg = new Argument<string>("ship-id", "The ship ID to view inventory for");
            var jsonOption = new Option<bool>("--json", "Output as raw JSON");

            var command = new Command("ship", "View ship cargo hold details") { shipIdArg };
            command.AddOption(jsonOption);

            command.SetHandler(async (string shipIdStr, bool json) =>
            {
                var corpId = _config.User.DefaultCorpId;
                if (!corpId.HasValue)
                {
                    System.Console.WriteLine("Error: No default corporation set. Use 'config set-corp <corp-id>' first.");
                    return;
                }

                if (!Guid.TryParse(shipIdStr, out var shipId))
                {
                    System.Console.WriteLine("Error: Invalid ship ID format.");
                    return;
                }

                var response = await _client.GetAsync($"/api/corp/{corpId.Value}/inventory/ship/{shipId}");
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    System.Console.WriteLine(content);
                    return;
                }

                if (json)
                {
                    System.Console.WriteLine(content);
                    return;
                }

                var inventory = JsonSerializer.Deserialize<ShipInventoryResponse>(content, JsonOptions);

                if (inventory == null)
                {
                    System.Console.WriteLine("Error: Unable to parse response.");
                    return;
                }

                System.Console.WriteLine();
                System.Console.WriteLine($"=== Ship: {inventory.ShipName} ===");
                System.Console.WriteLine($"Fleet: {inventory.FleetName}");
                System.Console.WriteLine($"Snapshot: {inventory.SnapshotTime:yyyy-MM-dd HH:mm:ss} UTC");
                System.Console.WriteLine();

                if (inventory.CargoHolds.Count == 0)
                {
                    System.Console.WriteLine("This ship has no cargo holds.");
                    return;
                }

                foreach (var hold in inventory.CargoHolds)
                {
                    var usedPercent = hold.Capacity > 0 ? (hold.Used / hold.Capacity * 100) : 0;
                    System.Console.WriteLine($"--- {hold.ModuleName} ---");
                    System.Console.WriteLine($"Capacity: {FormatQuantity(hold.Used)} / {FormatQuantity(hold.Capacity)} ({usedPercent:F1}%)");

                    if (hold.Contents.Count == 0)
                    {
                        System.Console.WriteLine("  [Empty]");
                    }
                    else
                    {
                        System.Console.WriteLine();
                        System.Console.WriteLine($"  {"Resource",-25} {"Class",-12} {"Quantity",15}");
                        System.Console.WriteLine($"  {new string('-', 54)}");

                        foreach (var item in hold.Contents.OrderByDescending(i => i.Quantity))
                        {
                            var classDisplay = item.AssetType == "Resource" ? item.ResourceClass : item.AssetType;
                            System.Console.WriteLine($"  {item.ResourceName,-25} {classDisplay,-12} {FormatQuantity(item.Quantity),15}");
                        }
                    }
                    System.Console.WriteLine();
                }
            }, shipIdArg, jsonOption);

            return command;
        }

        #endregion

        private static string FormatQuantity(decimal quantity)
        {
            return quantity.ToString("N3");
        }
    }
}
