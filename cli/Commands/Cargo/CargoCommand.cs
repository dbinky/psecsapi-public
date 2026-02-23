using System.CommandLine;
using System.Text.Json;
using psecsapi.Console.Infrastructure.Http;

namespace psecsapi.Console.Commands.Cargo;

public class CargoCommand : ICommand
{
    private readonly AuthenticatedHttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public CargoCommand(AuthenticatedHttpClient client)
    {
        _client = client;
    }

    public Command Build()
    {
        var command = new Command("cargo", "Cargo management operations");
        command.AddCommand(BuildTransferCommand());
        command.AddCommand(BuildMoveCommand());
        command.AddCommand(BuildInspectCommand());
        return command;
    }

    private Command BuildTransferCommand()
    {
        var assetIdArg = new Argument<Guid>("asset-id", "Boxed asset ID to transfer");
        var fromShipOption = new Option<Guid>("--from-ship", "Source ship ID") { IsRequired = true };
        var toShipOption = new Option<Guid>("--to-ship", "Destination ship ID") { IsRequired = true };
        var cargoOption = new Option<Guid>("--cargo", "Destination cargo module ID") { IsRequired = true };
        var fleetOption = new Option<Guid>("--fleet", "Fleet ID containing both ships") { IsRequired = true };
        var jsonOption = new Option<bool>("--json", "Output as raw JSON");

        var cmd = new Command("transfer", "Transfer cargo between ships in same fleet")
        {
            assetIdArg, fromShipOption, toShipOption, cargoOption, fleetOption, jsonOption
        };

        cmd.SetHandler(async (Guid assetId, Guid fromShip, Guid toShip, Guid cargo, Guid fleet, bool json) =>
        {
            var response = await _client.PostAsync($"/api/cargo/transfer?fleetId={fleet}", new
            {
                BoxedAssetId = assetId,
                SourceShipId = fromShip,
                DestinationShipId = toShip,
                DestinationCargoModuleId = cargo
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

            System.Console.WriteLine("Cargo transferred successfully.");
        }, assetIdArg, fromShipOption, toShipOption, cargoOption, fleetOption, jsonOption);

        return cmd;
    }

    private Command BuildMoveCommand()
    {
        var assetIdArg = new Argument<Guid>("asset-id", "Boxed asset ID to move");
        var shipOption = new Option<Guid>("--ship", "Ship ID") { IsRequired = true };
        var cargoOption = new Option<Guid>("--cargo", "Destination cargo module ID") { IsRequired = true };
        var jsonOption = new Option<bool>("--json", "Output as raw JSON");

        var cmd = new Command("move", "Move cargo between holds on same ship")
        {
            assetIdArg, shipOption, cargoOption, jsonOption
        };

        cmd.SetHandler(async (Guid assetId, Guid ship, Guid cargo, bool json) =>
        {
            var response = await _client.PostAsync($"/api/cargo/move?shipId={ship}", new
            {
                BoxedAssetId = assetId,
                DestinationCargoModuleId = cargo
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

            System.Console.WriteLine("Cargo moved successfully.");
        }, assetIdArg, shipOption, cargoOption, jsonOption);

        return cmd;
    }

    private Command BuildInspectCommand()
    {
        var assetIdArg = new Argument<Guid>("asset-id", "Boxed asset ID to inspect");
        var shipOption = new Option<Guid>("--ship", "Ship ID") { IsRequired = true };
        var jsonOption = new Option<bool>("--json", "Output as raw JSON");

        var cmd = new Command("inspect", "Inspect a cargo asset's properties")
        {
            assetIdArg, shipOption, jsonOption
        };

        cmd.SetHandler(async (Guid assetId, Guid ship, bool json) =>
        {
            var response = await _client.GetAsync($"/api/Ship/{ship}/cargo/{assetId}/inspect");

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

            var result = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);

            System.Console.WriteLine($"Asset: {result.GetProperty("name").GetString()}");
            System.Console.WriteLine($"  Type: {result.GetProperty("assetType").GetString()}");
            System.Console.WriteLine($"  ID: {result.GetProperty("assetId").GetString()}");
            System.Console.WriteLine($"  Quantity: {result.GetProperty("quantity").GetDecimal():F2}");
            System.Console.WriteLine($"  Mass: {result.GetProperty("mass").GetDecimal():F2}");

            if (result.TryGetProperty("resourceProperties", out var resProps) && resProps.ValueKind != JsonValueKind.Null)
            {
                System.Console.WriteLine("  Resource Properties:");
                foreach (var prop in resProps.EnumerateObject())
                    System.Console.WriteLine($"    {prop.Name}: {prop.Value.GetInt32()}");
            }

            if (result.TryGetProperty("componentQualities", out var compQ) && compQ.ValueKind != JsonValueKind.Null)
            {
                System.Console.WriteLine("  Quality Properties:");
                foreach (var prop in compQ.EnumerateObject())
                    System.Console.WriteLine($"    {prop.Name}: {prop.Value.GetDecimal():F2}");
            }

            if (result.TryGetProperty("tier", out var tier) && tier.ValueKind != JsonValueKind.Null)
                System.Console.WriteLine($"  Tier: {tier.GetInt32()}");

            if (result.TryGetProperty("category", out var cat) && cat.ValueKind != JsonValueKind.Null)
                System.Console.WriteLine($"  Category: {cat.GetString()}");

            if (result.TryGetProperty("definitionId", out var defId) && defId.ValueKind != JsonValueKind.Null)
                System.Console.WriteLine($"  Definition: {defId.GetString()}");

            if (result.TryGetProperty("slotType", out var slot) && slot.ValueKind != JsonValueKind.Null)
                System.Console.WriteLine($"  Slot Type: {slot.GetString()}");

            if (result.TryGetProperty("moduleCapabilities", out var caps) && caps.ValueKind != JsonValueKind.Null)
            {
                System.Console.WriteLine("  Capabilities:");
                foreach (var cap in caps.EnumerateArray())
                    System.Console.WriteLine($"    {cap.GetProperty("type").GetString()}: {cap.GetProperty("value").GetDecimal():F2}");
            }

            if (result.TryGetProperty("moduleRequirements", out var reqs) && reqs.ValueKind != JsonValueKind.Null)
            {
                System.Console.WriteLine("  Requirements:");
                foreach (var req in reqs.EnumerateArray())
                    System.Console.WriteLine($"    {req.GetProperty("type").GetString()}: {req.GetProperty("value").GetDecimal():F2}");
            }

            if (result.TryGetProperty("alloyProperties", out var alloyProps) && alloyProps.ValueKind != JsonValueKind.Null)
            {
                System.Console.WriteLine("  Alloy Properties:");
                foreach (var prop in alloyProps.EnumerateObject())
                    System.Console.WriteLine($"    {prop.Name}: {prop.Value.GetInt32()}");
            }
        }, assetIdArg, shipOption, jsonOption);

        return cmd;
    }
}
