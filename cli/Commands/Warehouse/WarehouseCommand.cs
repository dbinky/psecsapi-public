using System.CommandLine;
using System.Text.Json;
using psecsapi.Console.Infrastructure.Configuration;
using psecsapi.Console.Infrastructure.Http;

namespace psecsapi.Console.Commands.Warehouse;

public class WarehouseCommand : ICommand
{
    private readonly AuthenticatedHttpClient _client;
    private readonly CliConfig _config;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public WarehouseCommand(AuthenticatedHttpClient client, CliConfig config)
    {
        _client = client;
        _config = config;
    }

    public Command Build()
    {
        var command = new Command("warehouse", "Nexus Global Warehouse: store and retrieve assets");

        command.AddCommand(BuildListCommand());
        command.AddCommand(BuildSummaryCommand());
        command.AddCommand(BuildDepositCommand());
        command.AddCommand(BuildWithdrawCommand());

        return command;
    }

    // ── warehouse list ─────────────────────────────────────────────────

    private Command BuildListCommand()
    {
        var jsonOption = new Option<bool>("--json", "Output as raw JSON");

        var cmd = new Command("list", "Show all items stored in the warehouse") { jsonOption };
        cmd.SetHandler(async (bool json) =>
        {
            var response = await _client.GetAsync("/api/warehouse");
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

            var result = JsonSerializer.Deserialize<WarehouseContentsResponse>(content, JsonOptions);
            if (result == null)
            {
                System.Console.WriteLine("Error: Unable to parse response.");
                return;
            }

            System.Console.WriteLine();
            System.Console.WriteLine("=== Nexus Warehouse ===");
            System.Console.WriteLine();

            var freePct = result.FreeTierCapacity > 0
                ? result.FreeTierUsed / result.FreeTierCapacity * 100
                : 0;
            System.Console.WriteLine($"  Free Tier:  {result.FreeTierUsed:N0} / {result.FreeTierCapacity:N0} mass ({freePct:F0}%)");
            System.Console.WriteLine($"  Paid Tier:  {result.PaidTierUsed:N0} mass");
            System.Console.WriteLine($"  Total Mass: {result.TotalMassStored:N0} mass");
            System.Console.WriteLine();

            if (result.Items.Count == 0)
            {
                System.Console.WriteLine("  (No items stored)");
                System.Console.WriteLine();
                System.Console.WriteLine("Deposit assets with: papi warehouse deposit <asset-id> --asset-type <type> --fleet <fleet-id> --ship <ship-id>");
                System.Console.WriteLine();
                return;
            }

            System.Console.WriteLine($"  {"Asset ID",-38} {"Type",-12} {"Name",-24} {"Mass",8} {"Tier",-6} {"Rate/Day",9}  Deposited");
            System.Console.WriteLine($"  {new string('-', 110)}");

            foreach (var item in result.Items)
            {
                var name = item.AssetName.Length > 23
                    ? item.AssetName[..20] + "..."
                    : item.AssetName;
                var tier = item.Tier == "Free" ? "Free" : "Paid";
                var inGrace = item.GracePeriodStart.HasValue ? " [GRACE]" : "";

                System.Console.WriteLine(
                    $"  {item.AssetId,-38} {item.AssetType,-12} {name,-24} {item.Mass,8:N0} {tier,-6} {item.DailyRate,9:N2}{inGrace}  {item.DepositedAt:yyyy-MM-dd HH:mm}");
            }

            System.Console.WriteLine();
            System.Console.WriteLine($"  {result.Items.Count} item(s) stored");
            System.Console.WriteLine();
        }, jsonOption);

        return cmd;
    }

    // ── warehouse summary ──────────────────────────────────────────────

    private Command BuildSummaryCommand()
    {
        var jsonOption = new Option<bool>("--json", "Output as raw JSON");

        var cmd = new Command("summary", "Show warehouse billing summary") { jsonOption };
        cmd.SetHandler(async (bool json) =>
        {
            var response = await _client.GetAsync("/api/warehouse/summary");
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

            var result = JsonSerializer.Deserialize<WarehouseSummaryResponse>(content, JsonOptions);
            if (result == null)
            {
                System.Console.WriteLine("Error: Unable to parse response.");
                return;
            }

            System.Console.WriteLine();
            System.Console.WriteLine("=== Warehouse Summary ===");
            System.Console.WriteLine();
            System.Console.WriteLine($"  Total Items:       {result.TotalItems}");
            System.Console.WriteLine($"  Total Mass Stored: {result.TotalMassStored:N0} mass");
            System.Console.WriteLine();
            System.Console.WriteLine("Free Tier:");

            var freePct = result.FreeTierCapacity > 0
                ? result.FreeTierUsed / result.FreeTierCapacity * 100
                : 0;
            System.Console.WriteLine($"  Capacity: {result.FreeTierCapacity:N0} mass");
            System.Console.WriteLine($"  Used:     {result.FreeTierUsed:N0} mass ({freePct:F0}%)");
            System.Console.WriteLine();
            System.Console.WriteLine("Paid Tier:");
            System.Console.WriteLine($"  Used:              {result.PaidTierUsed:N0} mass");
            System.Console.WriteLine($"  Daily Billing:     {result.DailyBillingTotal:N2} credits/day");

            if (result.ItemsInGracePeriod > 0)
            {
                System.Console.WriteLine();
                System.Console.WriteLine($"  Items in Grace Period: {result.ItemsInGracePeriod} (payment overdue — retrieve soon!)");
            }

            System.Console.WriteLine();
        }, jsonOption);

        return cmd;
    }

    // ── warehouse deposit ──────────────────────────────────────────────

    private Command BuildDepositCommand()
    {
        var assetIdArg = new Argument<Guid>("asset-id", "Boxed asset ID to deposit");
        var assetTypeOption = new Option<string>("--asset-type", "Asset type: Resource, TechModule, Alloy, Component, Chassis") { IsRequired = true };
        assetTypeOption.AddAlias("-t");
        var fleetOption = new Option<Guid>("--fleet", "Fleet ID containing the source ship") { IsRequired = true };
        fleetOption.AddAlias("-f");
        var shipOption = new Option<Guid>("--ship", "Ship ID containing the asset") { IsRequired = true };
        shipOption.AddAlias("-s");
        var pickupDaysOption = new Option<int?>("--pickup-days", "Paid tier: days to reserve the pickup window (min 1). If omitted, uses free tier.");
        var jsonOption = new Option<bool>("--json", "Output as raw JSON");

        var cmd = new Command("deposit", "Deposit a boxed asset from ship cargo into the warehouse")
        {
            assetIdArg, assetTypeOption, fleetOption, shipOption, pickupDaysOption, jsonOption
        };

        cmd.SetHandler(async (Guid assetId, string assetType, Guid fleetId, Guid shipId, int? pickupDays, bool json) =>
        {
            var request = new
            {
                AssetId = assetId,
                AssetType = assetType,
                FleetId = fleetId,
                ShipId = shipId,
                PickupWindowDays = pickupDays
            };

            var response = await _client.PostAsync("/api/warehouse/deposit", request);
            var content = await response.Content.ReadAsStringAsync();

            if (json)
            {
                System.Console.WriteLine(content);
                return;
            }

            var result = JsonSerializer.Deserialize<WarehouseDepositResponse>(content, JsonOptions);
            if (result == null)
            {
                System.Console.WriteLine(content);
                return;
            }

            if (!result.Success)
            {
                System.Console.WriteLine($"Deposit failed: {result.ErrorMessage}");
                return;
            }

            System.Console.WriteLine();
            System.Console.WriteLine("Asset deposited!");
            System.Console.WriteLine($"  Tier:             {result.Tier}");
            System.Console.WriteLine($"  Mass Deposited:   {result.MassDeposited:N0} mass");

            if (result.CreditsCharged > 0)
                System.Console.WriteLine($"  Credits Charged:  {result.CreditsCharged:N2} credits");
            else
                System.Console.WriteLine("  Credits Charged:  0 (free tier)");

            System.Console.WriteLine();
            System.Console.WriteLine("Retrieve with: papi warehouse withdraw <asset-id> --fleet <fleet-id> --ship <ship-id> --cargo <cargo-module-id>");
            System.Console.WriteLine();
        }, assetIdArg, assetTypeOption, fleetOption, shipOption, pickupDaysOption, jsonOption);

        return cmd;
    }

    // ── warehouse withdraw ─────────────────────────────────────────────

    private Command BuildWithdrawCommand()
    {
        var assetIdArg = new Argument<Guid>("asset-id", "Asset ID to retrieve from warehouse");
        var fleetOption = new Option<Guid>("--fleet", "Fleet ID containing the destination ship") { IsRequired = true };
        fleetOption.AddAlias("-f");
        var shipOption = new Option<Guid>("--ship", "Ship ID to receive the asset") { IsRequired = true };
        shipOption.AddAlias("-s");
        var cargoOption = new Option<Guid>("--cargo", "Cargo module ID on the destination ship") { IsRequired = true };
        cargoOption.AddAlias("-c");
        var jsonOption = new Option<bool>("--json", "Output as raw JSON");

        var cmd = new Command("withdraw", "Withdraw an asset from the warehouse into ship cargo")
        {
            assetIdArg, fleetOption, shipOption, cargoOption, jsonOption
        };

        cmd.SetHandler(async (Guid assetId, Guid fleetId, Guid shipId, Guid cargoModuleId, bool json) =>
        {
            var request = new
            {
                AssetId = assetId,
                FleetId = fleetId,
                ShipId = shipId,
                CargoModuleId = cargoModuleId
            };

            var response = await _client.PostAsync("/api/warehouse/withdraw", request);
            var content = await response.Content.ReadAsStringAsync();

            if (json)
            {
                System.Console.WriteLine(content);
                return;
            }

            var result = JsonSerializer.Deserialize<WarehouseWithdrawResponse>(content, JsonOptions);
            if (result == null)
            {
                System.Console.WriteLine(content);
                return;
            }

            if (!result.Success)
            {
                System.Console.WriteLine($"Withdrawal failed: {result.ErrorMessage}");
                return;
            }

            System.Console.WriteLine();
            System.Console.WriteLine("Asset withdrawn from warehouse!");

            if (result.CreditsRefunded > 0)
                System.Console.WriteLine($"  Credits Refunded:  {result.CreditsRefunded:N2} credits (unused paid tier time)");

            if (result.ItemsPromoted > 0)
            {
                System.Console.WriteLine($"  Items Promoted:    {result.ItemsPromoted} paid-tier item(s) moved to free tier");
                if (result.PromotionRefund > 0)
                    System.Console.WriteLine($"  Promotion Refund:  {result.PromotionRefund:N2} credits");
            }

            System.Console.WriteLine();
            System.Console.WriteLine($"Check cargo with: papi ship cargo {shipId}");
            System.Console.WriteLine();
        }, assetIdArg, fleetOption, shipOption, cargoOption, jsonOption);

        return cmd;
    }

    // ── Response models ────────────────────────────────────────────────

    private class WarehouseContentsResponse
    {
        public List<WarehouseItem> Items { get; set; } = new();
        public decimal TotalMassStored { get; set; }
        public decimal FreeTierCapacity { get; set; }
        public decimal FreeTierUsed { get; set; }
        public decimal PaidTierUsed { get; set; }
    }

    private class WarehouseItem
    {
        public Guid AssetId { get; set; }
        public string AssetType { get; set; } = string.Empty;
        public string AssetName { get; set; } = string.Empty;
        public decimal Mass { get; set; }
        public string Tier { get; set; } = string.Empty;
        public DateTime DepositedAt { get; set; }
        public decimal DailyRate { get; set; }
        public int PickupWindowDays { get; set; }
        public decimal DepositPaid { get; set; }
        public decimal DepositRemaining { get; set; }
        public DateTime? GracePeriodStart { get; set; }
    }

    private class WarehouseSummaryResponse
    {
        public int TotalItems { get; set; }
        public decimal TotalMassStored { get; set; }
        public decimal FreeTierCapacity { get; set; }
        public decimal FreeTierUsed { get; set; }
        public decimal PaidTierUsed { get; set; }
        public decimal DailyBillingTotal { get; set; }
        public int ItemsInGracePeriod { get; set; }
    }

    private class WarehouseDepositResponse
    {
        public bool Success { get; set; }
        public string Tier { get; set; } = string.Empty;
        public decimal MassDeposited { get; set; }
        public decimal CreditsCharged { get; set; }
        public string? ErrorMessage { get; set; }
    }

    private class WarehouseWithdrawResponse
    {
        public bool Success { get; set; }
        public decimal CreditsRefunded { get; set; }
        public int ItemsPromoted { get; set; }
        public decimal PromotionRefund { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
