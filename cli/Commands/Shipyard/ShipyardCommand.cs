using System.CommandLine;
using System.Text.Json;
using psecsapi.Console.Infrastructure.Configuration;
using psecsapi.Console.Infrastructure.Http;

namespace psecsapi.Console.Commands.Shipyard;

public class ShipyardCommand : ICommand
{
    private readonly AuthenticatedHttpClient _client;
    private readonly CliConfig _config;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ShipyardCommand(AuthenticatedHttpClient client, CliConfig config)
    {
        _client = client;
        _config = config;
    }

    public Command Build()
    {
        var command = new Command("shipyard", "Nexus station shipyard operations");

        command.AddCommand(BuildQueueCommand());
        command.AddCommand(BuildCompletedCommand());
        command.AddCommand(BuildBuildCommand());
        command.AddCommand(BuildCancelCommand());
        command.AddCommand(BuildPickupCommand());
        command.AddCommand(BuildBlueprintCommand());

        return command;
    }

    private Command BuildQueueCommand()
    {
        var jsonOption = new Option<bool>("--json", "Output as raw JSON");

        var cmd = new Command("queue", "Show shipyard build queue") { jsonOption };
        cmd.SetHandler(async (bool json) =>
        {
            var response = await _client.GetAsync("/api/shipyard/queue");

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

            var queue = JsonSerializer.Deserialize<QueueResponse>(content, JsonOptions);
            if (queue == null)
            {
                System.Console.WriteLine("Error: Unable to parse response.");
                return;
            }

            System.Console.WriteLine($"Shipyard Queue (depth: {queue.TotalQueueDepth})");
            System.Console.WriteLine();

            if (queue.CurrentBuild != null)
            {
                System.Console.WriteLine("  Current Build:");
                PrintQueueEntry(queue.CurrentBuild);
            }

            if (queue.QueuedBuilds.Count > 0)
            {
                System.Console.WriteLine("  Queued:");
                foreach (var entry in queue.QueuedBuilds)
                    PrintQueueEntry(entry);
            }
        }, jsonOption);

        return cmd;
    }

    private Command BuildCompletedCommand()
    {
        var jsonOption = new Option<bool>("--json", "Output as raw JSON");

        var cmd = new Command("completed", "Show completed builds awaiting pickup") { jsonOption };
        cmd.SetHandler(async (bool json) =>
        {
            var response = await _client.GetAsync("/api/shipyard/completed");

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

            var result = JsonSerializer.Deserialize<CompletedOrdersResponse>(content, JsonOptions);
            if (result == null)
            {
                System.Console.WriteLine("Error: Unable to parse response.");
                return;
            }

            if (result.Orders.Count == 0)
            {
                System.Console.WriteLine("No completed builds awaiting pickup.");
                return;
            }

            System.Console.WriteLine($"Completed Builds Ready for Pickup ({result.Total}):");
            System.Console.WriteLine();
            foreach (var order in result.Orders)
            {
                System.Console.WriteLine($"  Order #{order.OrderNumber} — {order.ChassisName} ({order.TotalSlots} slots, quality {order.BlueprintQuality:F2})");
                System.Console.WriteLine($"    Completed: {order.CompletedTimestamp:yyyy-MM-dd HH:mm:ss}");
                System.Console.WriteLine($"    Pickup: shipyard pickup {order.OrderNumber} --fleet <fleet-id>");
            }
            System.Console.WriteLine();
        }, jsonOption);

        return cmd;
    }

    private Command BuildBuildCommand()
    {
        var catalogIdArg = new Argument<string>("catalog-id", "Ship catalog configuration ID");
        var blueprintOption = new Option<Guid>("--blueprint", "Blueprint instance ID") { IsRequired = true };
        var inputOption = new Option<string[]>("--input", "Input resources (label:boxed-asset-id)")
        {
            IsRequired = true,
            AllowMultipleArgumentsPerToken = true
        };
        var jsonOption = new Option<bool>("--json", "Output as raw JSON");

        var cmd = new Command("build", "Queue a chassis build order")
        {
            catalogIdArg, blueprintOption, inputOption, jsonOption
        };

        cmd.SetHandler(async (string catalogId, Guid blueprintId, string[] inputs, bool json) =>
        {
            var selectedInputs = new Dictionary<string, List<Guid>>();
            foreach (var input in inputs)
            {
                var parts = input.Split(':');
                if (parts.Length != 2)
                {
                    System.Console.WriteLine($"Invalid input format: {input} (expected label:guid)");
                    return;
                }
                var label = parts[0];
                if (!Guid.TryParse(parts[1], out var assetId))
                {
                    System.Console.WriteLine($"Invalid GUID in input: {parts[1]}");
                    return;
                }
                if (!selectedInputs.ContainsKey(label))
                    selectedInputs[label] = new();
                selectedInputs[label].Add(assetId);
            }

            var response = await _client.PostAsync("/api/shipyard/build", new
            {
                CatalogId = catalogId,
                BlueprintInstanceId = blueprintId,
                SelectedInputs = selectedInputs
            });

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

            var result = JsonSerializer.Deserialize<BuildOrderResponse>(content, JsonOptions);
            if (result == null)
            {
                System.Console.WriteLine("Error: Unable to parse response.");
                return;
            }

            System.Console.WriteLine("Build order placed!");
            System.Console.WriteLine($"  Order #: {result.OrderNumber}");
            System.Console.WriteLine($"  Queue Position: {result.QueuePosition}");
            System.Console.WriteLine($"  Work Units: {result.TotalWorkUnits}");
            System.Console.WriteLine($"  Est. Minutes: {result.EstimatedMinutes}");
            System.Console.WriteLine($"  Build Fee: {result.BuildFee}");
        }, catalogIdArg, blueprintOption, inputOption, jsonOption);

        return cmd;
    }

    private Command BuildCancelCommand()
    {
        var orderNumberArg = new Argument<long>("order-number", "Build order number");
        var jsonOption = new Option<bool>("--json", "Output as raw JSON");

        var cmd = new Command("cancel", "Cancel a queued build order")
        {
            orderNumberArg, jsonOption
        };

        cmd.SetHandler(async (long orderNumber, bool json) =>
        {
            var response = await _client.DeleteAsync($"/api/shipyard/build/{orderNumber}");

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

            var result = JsonSerializer.Deserialize<CancelBuildOrderResponse>(content, JsonOptions);
            System.Console.WriteLine("Build order cancelled successfully.");
            if (result?.ReturnedAssetIds is { Count: > 0 } assets)
            {
                System.Console.WriteLine($"Input assets returned to your warehouse ({assets.Count} item(s)):");
                foreach (var assetId in assets)
                    System.Console.WriteLine($"  {assetId}");
                System.Console.WriteLine("Use 'papi warehouse list' to view them or 'papi warehouse withdraw' to move them to a ship.");
            }
        }, orderNumberArg, jsonOption);

        return cmd;
    }

    private Command BuildPickupCommand()
    {
        var orderNumberArg = new Argument<long>("order-number", "Build order number");
        var fleetOption = new Option<Guid>("--fleet", "Fleet ID to receive the ship") { IsRequired = true };
        var jsonOption = new Option<bool>("--json", "Output as raw JSON");

        var cmd = new Command("pickup", "Retrieve a completed chassis")
        {
            orderNumberArg, fleetOption, jsonOption
        };

        cmd.SetHandler(async (long orderNumber, Guid fleetId, bool json) =>
        {
            var response = await _client.PostAsync(
                $"/api/shipyard/pickup/{orderNumber}?fleetId={fleetId}", null);

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

            var result = JsonSerializer.Deserialize<PickupResponse>(content, JsonOptions);
            if (result?.ShipDetail == null)
            {
                System.Console.WriteLine("Error: Unable to parse response.");
                return;
            }

            var sd = result.ShipDetail;
            var totalSlots = sd.TotalInteriorSlots + sd.TotalExteriorSlots;
            var hasModules = sd.Modules != null && sd.Modules.Count > 0;

            System.Console.WriteLine("Chassis Retrieved");
            System.Console.WriteLine();
            System.Console.WriteLine($"  New Ship: {sd.Name}");
            System.Console.WriteLine($"  Ship ID: {sd.EntityId}");
            System.Console.WriteLine($"  Class: {sd.Class} ({totalSlots} slots: {sd.TotalInteriorSlots} interior, {sd.TotalExteriorSlots} exterior)");
            System.Console.WriteLine($"  Structure Points: {sd.CurrentStructurePoints:F0} / {sd.MaxStructurePoints:F0}");
            System.Console.WriteLine($"  Hull Points: {sd.CurrentHullPoints:F0} / {sd.MaxHullPoints:F0}");
            System.Console.WriteLine($"  Modules: {(hasModules ? string.Join(", ", sd.Modules!.Select(m => m.Name)) : "None — ship requires outfitting before use")}");
            System.Console.WriteLine($"  Added to fleet: {sd.FleetId}");

            if (!hasModules)
            {
                System.Console.WriteLine();
                System.Console.WriteLine("  Warning: This ship has no modules and cannot navigate, scan, or extract.");
                System.Console.WriteLine("  Use 'ship install' to equip modules.");
            }
        }, orderNumberArg, fleetOption, jsonOption);

        return cmd;
    }

    private Command BuildBlueprintCommand()
    {
        var blueprintIdArg = new Argument<string>("blueprint-id", "Chassis blueprint definition ID (e.g., scout-chassis)");
        var interiorOption = new Option<int?>("--interior-slots", "Interior slot count for total cost calculation");
        var exteriorOption = new Option<int?>("--exterior-slots", "Exterior slot count for total cost calculation");
        var jsonOption = new Option<bool>("--json", "Output as raw JSON");

        var cmd = new Command("blueprint", "Inspect a chassis blueprint recipe")
        {
            blueprintIdArg, interiorOption, exteriorOption, jsonOption
        };

        cmd.SetHandler(async (string blueprintId, int? interiorSlots, int? exteriorSlots, bool json) =>
        {
            var url = $"/api/shipyard/blueprint/{Uri.EscapeDataString(blueprintId)}";
            if (interiorSlots.HasValue && exteriorSlots.HasValue)
                url += $"?interiorSlots={interiorSlots.Value}&exteriorSlots={exteriorSlots.Value}";

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

            var bp = JsonSerializer.Deserialize<ChassisBlueprintDetail>(content, JsonOptions);
            if (bp == null)
            {
                System.Console.WriteLine("Error: Unable to parse response.");
                return;
            }

            System.Console.WriteLine($"Chassis Blueprint: {bp.BlueprintId}");
            System.Console.WriteLine($"  Class: {bp.ChassisClass}");
            System.Console.WriteLine($"  Work Units/Slot: {bp.BaseWorkUnitsPerSlot}");
            System.Console.WriteLine();

            if (bp.BaseInputResources.Count > 0)
            {
                System.Console.WriteLine("  Base Resources:");
                foreach (var r in bp.BaseInputResources)
                    System.Console.WriteLine($"    {r.Label}: {r.Quantity}x ({r.Qualifier}/{r.Value})");
            }
            if (bp.BaseInputComponents.Count > 0)
            {
                System.Console.WriteLine("  Base Components:");
                foreach (var c in bp.BaseInputComponents)
                    System.Console.WriteLine($"    {c.Label}: {c.Quantity}x [{c.ComponentType}]");
            }

            if (bp.PerInteriorSlotInputResources.Count > 0)
            {
                System.Console.WriteLine("  Per Interior Slot Resources:");
                foreach (var r in bp.PerInteriorSlotInputResources)
                    System.Console.WriteLine($"    {r.Label}: {r.Quantity}x ({r.Qualifier}/{r.Value})");
            }
            if (bp.PerInteriorSlotInputComponents.Count > 0)
            {
                System.Console.WriteLine("  Per Interior Slot Components:");
                foreach (var c in bp.PerInteriorSlotInputComponents)
                    System.Console.WriteLine($"    {c.Label}: {c.Quantity}x [{c.ComponentType}]");
            }

            if (bp.PerExteriorSlotInputResources.Count > 0)
            {
                System.Console.WriteLine("  Per Exterior Slot Resources:");
                foreach (var r in bp.PerExteriorSlotInputResources)
                    System.Console.WriteLine($"    {r.Label}: {r.Quantity}x ({r.Qualifier}/{r.Value})");
            }
            if (bp.PerExteriorSlotInputComponents.Count > 0)
            {
                System.Console.WriteLine("  Per Exterior Slot Components:");
                foreach (var c in bp.PerExteriorSlotInputComponents)
                    System.Console.WriteLine($"    {c.Label}: {c.Quantity}x [{c.ComponentType}]");
            }

            if (bp.CalculatedTotalResources != null || bp.CalculatedTotalComponents != null)
            {
                System.Console.WriteLine();
                System.Console.WriteLine($"  === Calculated Totals (interior={interiorSlots}, exterior={exteriorSlots}) ===");

                if (bp.CalculatedTotalWorkUnits.HasValue)
                    System.Console.WriteLine($"  Total Work Units: {bp.CalculatedTotalWorkUnits.Value}");

                if (bp.CalculatedTotalResources != null)
                {
                    System.Console.WriteLine("  Total Resources:");
                    foreach (var r in bp.CalculatedTotalResources)
                        System.Console.WriteLine($"    {r.Label}: {r.Quantity}x ({r.Qualifier}/{r.Value})");
                }
                if (bp.CalculatedTotalComponents != null)
                {
                    System.Console.WriteLine("  Total Components:");
                    foreach (var c in bp.CalculatedTotalComponents)
                        System.Console.WriteLine($"    {c.Label}: {c.Quantity}x [{c.ComponentType}]");
                }
            }
        }, blueprintIdArg, interiorOption, exteriorOption, jsonOption);

        return cmd;
    }

    private static void PrintQueueEntry(QueueEntry entry)
    {
        var own = entry.IsOwnOrder ? " (yours)" : "";
        System.Console.WriteLine($"    Order #{entry.OrderNumber}{own} — {entry.TotalSlots} slots, {entry.ProgressPercent:F1}% complete, ~{entry.EstimatedMinutesRemaining:F0} min remaining");
    }

    private class QueueResponse
    {
        public QueueEntry? CurrentBuild { get; set; }
        public List<QueueEntry> QueuedBuilds { get; set; } = new();
        public int TotalQueueDepth { get; set; }
    }

    private class QueueEntry
    {
        public long OrderNumber { get; set; }
        public int TotalSlots { get; set; }
        public decimal ProgressPercent { get; set; }
        public decimal EstimatedMinutesRemaining { get; set; }
        public bool IsOwnOrder { get; set; }
        public DateTime PlacedTimestamp { get; set; }
    }

    private class BuildOrderResponse
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public long OrderNumber { get; set; }
        public decimal TotalWorkUnits { get; set; }
        public int QueuePosition { get; set; }
        public decimal EstimatedMinutes { get; set; }
        public decimal BuildFee { get; set; }
    }

    private class ChassisBlueprintDetail
    {
        public string BlueprintId { get; set; } = "";
        public string ChassisClass { get; set; } = "";
        public int BaseWorkUnitsPerSlot { get; set; }
        public List<ChassisBlueprintResource> BaseInputResources { get; set; } = new();
        public List<ChassisBlueprintComponent> BaseInputComponents { get; set; } = new();
        public List<ChassisBlueprintResource> PerInteriorSlotInputResources { get; set; } = new();
        public List<ChassisBlueprintComponent> PerInteriorSlotInputComponents { get; set; } = new();
        public List<ChassisBlueprintResource> PerExteriorSlotInputResources { get; set; } = new();
        public List<ChassisBlueprintComponent> PerExteriorSlotInputComponents { get; set; } = new();
        public List<ChassisBlueprintResource>? CalculatedTotalResources { get; set; }
        public List<ChassisBlueprintComponent>? CalculatedTotalComponents { get; set; }
        public decimal? CalculatedTotalWorkUnits { get; set; }
    }

    private class ChassisBlueprintResource
    {
        public string Label { get; set; } = "";
        public string Qualifier { get; set; } = "";
        public string Value { get; set; } = "";
        public int Quantity { get; set; }
    }

    private class ChassisBlueprintComponent
    {
        public string Label { get; set; } = "";
        public string ComponentType { get; set; } = "";
        public int Quantity { get; set; }
    }

    private class PickupResponse
    {
        public bool Success { get; set; }
        public Guid ShipId { get; set; }
        public ShipDetailModel? ShipDetail { get; set; }
    }

    private class ShipDetailModel
    {
        public Guid EntityId { get; set; }
        public string Name { get; set; } = "";
        public string Class { get; set; } = "";
        public int TotalInteriorSlots { get; set; }
        public int TotalExteriorSlots { get; set; }
        public decimal? CurrentStructurePoints { get; set; }
        public decimal MaxStructurePoints { get; set; }
        public decimal? CurrentHullPoints { get; set; }
        public decimal MaxHullPoints { get; set; }
        public Guid FleetId { get; set; }
        public List<ModuleModel>? Modules { get; set; }
    }

    private class ModuleModel
    {
        public string Name { get; set; } = "";
    }

    private class CancelBuildOrderResponse
    {
        public bool Success { get; set; }
        public List<Guid> ReturnedAssetIds { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    private class CompletedOrdersResponse
    {
        public List<CompletedOrderEntry> Orders { get; set; } = new();
        public int Total { get; set; }
    }

    private class CompletedOrderEntry
    {
        public long OrderNumber { get; set; }
        public string ChassisName { get; set; } = "";
        public string CatalogId { get; set; } = "";
        public decimal BlueprintQuality { get; set; }
        public int InteriorSlots { get; set; }
        public int ExteriorSlots { get; set; }
        public int TotalSlots { get; set; }
        public DateTime CompletedTimestamp { get; set; }
    }
}
