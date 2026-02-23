using System.CommandLine;
using System.Text.Json;
using psecsapi.Console.Infrastructure.Configuration;
using psecsapi.Console.Infrastructure.Http;

namespace psecsapi.Console.Commands.Ship
{
    public class ShipCommand : ICommand
    {
        private readonly AuthenticatedHttpClient _client;
        private readonly CliConfig _config;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public ShipCommand(AuthenticatedHttpClient client, CliConfig config)
        {
            _client = client;
            _config = config;
        }

        public Command Build()
        {
            var command = new Command("ship", "Ship management commands");

            command.AddCommand(BuildGetCommand());
            command.AddCommand(BuildExtractCommand());
            command.AddCommand(BuildExtractStopCommand());
            command.AddCommand(BuildExtractStatusCommand());
            command.AddCommand(new ShipCatalogSubCommand(_client).Build());
            command.AddCommand(BuildCargoCommand());
            command.AddCommand(BuildInstallCommand());
            command.AddCommand(BuildUninstallCommand());

            return command;
        }

        private Command BuildGetCommand()
        {
            var shipIdArg = new Argument<Guid>("ship-id", "Ship ID");
            var jsonOption = new Option<bool>("--json", "Output as raw JSON");

            var command = new Command("get", "Get ship details") { shipIdArg, jsonOption };
            command.SetHandler(async (shipId, json) =>
            {
                var response = await _client.GetAsync($"/api/Ship/{shipId}");
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

                FormatShipDetail(content);
            }, shipIdArg, jsonOption);

            return command;
        }

        private Command BuildExtractCommand()
        {
            var shipIdArg = new Argument<Guid>("ship-id", "Ship ID");
            var resourceOption = new Option<Guid>("--resource", "Resource ID to extract (get from 'fleet scan deep')") { IsRequired = true };
            resourceOption.AddAlias("-r");
            var limitOption = new Option<decimal?>("--limit", "Optional quantity limit");
            limitOption.AddAlias("-l");

            var command = new Command("extract", "Start extraction of a resource") { shipIdArg, resourceOption, limitOption };
            command.SetHandler(async (shipId, resourceId, quantityLimit) =>
            {
                var requestBody = new { resourceId, quantityLimit };
                var response = await _client.PostAsync($"/api/Ship/{shipId}/extraction", requestBody);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Console.WriteLine($"Error: {response.StatusCode}");
                    System.Console.WriteLine(errorContent);
                    return;
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ExtractionJobStatusResponse>(content, JsonOptions);

                if (result == null)
                {
                    System.Console.WriteLine("Error: Unable to parse response.");
                    return;
                }

                System.Console.WriteLine("Extraction started!");
                System.Console.WriteLine();
                System.Console.WriteLine($"  Job ID: {result.JobId}");
                System.Console.WriteLine($"  Resource: {result.ResourceName}");
                System.Console.WriteLine($"  Rate: {result.RatePerMinute:F2} units/min");
                if (result.QuantityLimit.HasValue)
                {
                    System.Console.WriteLine($"  Limit: {result.QuantityLimit.Value:F2} units");
                }
                System.Console.WriteLine();
                System.Console.WriteLine($"  Monitor progress: ship extract-status {shipId}");
                System.Console.WriteLine($"  Stop and collect: ship extract-stop {shipId} --job {result.JobId}");
            }, shipIdArg, resourceOption, limitOption);

            return command;
        }

        private Command BuildExtractStopCommand()
        {
            var shipIdArg = new Argument<Guid>("ship-id", "Ship ID");
            var jobOption = new Option<Guid?>("--job", "Specific job ID to stop (stops all if not specified)");
            jobOption.AddAlias("-j");

            var command = new Command("extract-stop", "Stop extraction job(s)") { shipIdArg, jobOption };
            command.SetHandler(async (shipId, jobId) =>
            {
                HttpResponseMessage response;

                if (jobId.HasValue)
                {
                    response = await _client.DeleteAsync($"/api/Ship/{shipId}/extraction?jobId={jobId.Value}");
                }
                else
                {
                    response = await _client.DeleteAsync($"/api/Ship/{shipId}/extraction/all");
                }

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Console.WriteLine($"Error: {response.StatusCode}");
                    System.Console.WriteLine(errorContent);
                    return;
                }

                var content = await response.Content.ReadAsStringAsync();

                if (jobId.HasValue)
                {
                    var result = JsonSerializer.Deserialize<MaterializationResultResponse>(content, JsonOptions);
                    if (result == null)
                    {
                        System.Console.WriteLine("Error: Unable to parse response.");
                        return;
                    }

                    System.Console.WriteLine("Extraction stopped!");
                    System.Console.WriteLine();
                    PrintMaterializationResult(result);
                }
                else
                {
                    var results = JsonSerializer.Deserialize<List<MaterializationResultResponse>>(content, JsonOptions);
                    if (results == null)
                    {
                        System.Console.WriteLine("Error: Unable to parse response.");
                        return;
                    }

                    if (results.Count == 0)
                    {
                        System.Console.WriteLine("No active extractions to stop.");
                        return;
                    }

                    System.Console.WriteLine($"Stopped {results.Count} extraction(s)!");
                    System.Console.WriteLine();

                    foreach (var result in results)
                    {
                        PrintMaterializationResult(result);
                        System.Console.WriteLine();
                    }
                }
            }, shipIdArg, jobOption);

            return command;
        }

        private Command BuildExtractStatusCommand()
        {
            var shipIdArg = new Argument<Guid>("ship-id", "Ship ID");

            var command = new Command("extract-status", "Get extraction status") { shipIdArg };
            command.SetHandler(async (shipId) =>
            {
                var response = await _client.GetAsync($"/api/Ship/{shipId}/extraction");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Console.WriteLine($"Error: {response.StatusCode}");
                    System.Console.WriteLine(errorContent);
                    return;
                }

                var content = await response.Content.ReadAsStringAsync();
                var results = JsonSerializer.Deserialize<List<ExtractionJobStatusResponse>>(content, JsonOptions);

                if (results == null)
                {
                    System.Console.WriteLine("Error: Unable to parse response.");
                    return;
                }

                if (results.Count == 0)
                {
                    System.Console.WriteLine("No active extractions.");
                    return;
                }

                System.Console.WriteLine($"Active extractions: {results.Count}");
                System.Console.WriteLine();

                foreach (var job in results)
                {
                    System.Console.WriteLine($"  Job: {job.JobId.ToString()[..8]}...");
                    System.Console.WriteLine($"  Resource: {job.ResourceName}");
                    System.Console.WriteLine($"  Rate: {job.RatePerMinute:F2} units/min");
                    System.Console.WriteLine($"  Extracted: {job.AccumulatedQuantity:F2} units");
                    if (job.QuantityLimit.HasValue)
                    {
                        System.Console.WriteLine($"  Limit: {job.QuantityLimit.Value:F2} units");
                    }
                    System.Console.WriteLine();
                }
            }, shipIdArg);

            return command;
        }

        private Command BuildCargoCommand()
        {
            var shipIdArg = new Argument<Guid>("ship-id", "The ship ID to list cargo for");
            var jsonOption = new Option<bool>("--json", () => false, "Output as JSON");

            var command = new Command("cargo", "List cargo hold contents")
            {
                shipIdArg, jsonOption
            };

            command.SetHandler(async (shipId, json) =>
            {
                var response = await _client.GetAsync($"/api/Ship/{shipId}/cargo");
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

                var items = JsonSerializer.Deserialize<List<CargoItem>>(content, JsonOptions);
                if (items == null || items.Count == 0)
                {
                    System.Console.WriteLine("Cargo hold is empty.");
                    return;
                }

                System.Console.WriteLine();
                System.Console.WriteLine($"{"Asset ID",-38} {"Type",-12} {"Name",-24} {"Quantity",12} {"Mass",10}");
                System.Console.WriteLine(new string('-', 100));

                foreach (var item in items)
                {
                    var name = item.Name.Length > 23
                        ? item.Name[..20] + "..."
                        : item.Name;

                    System.Console.WriteLine($"{item.AssetId,-38} {item.AssetType,-12} {name,-24} {item.Quantity,12:N1} {item.Mass,10:N1}");
                }

                System.Console.WriteLine();
                System.Console.WriteLine($"{items.Count} item(s) in cargo.");

            }, shipIdArg, jsonOption);

            return command;
        }

        private Command BuildInstallCommand()
        {
            var shipIdArg = new Argument<Guid>("ship-id", "Ship ID");
            var moduleOption = new Option<Guid[]>("--module", "Boxed module asset IDs to install")
            {
                IsRequired = true,
                AllowMultipleArgumentsPerToken = true
            };
            var jsonOption = new Option<bool>("--json", "Output as raw JSON");

            var cmd = new Command("install", "Install modules onto ship")
            {
                shipIdArg, moduleOption, jsonOption
            };

            cmd.SetHandler(async (Guid shipId, Guid[] moduleIds, bool json) =>
            {
                var response = await _client.PostAsync($"/api/Ship/{shipId}/install", new
                {
                    BoxedModuleIds = moduleIds.ToList()
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

                var result = JsonSerializer.Deserialize<InstallResponse>(content, JsonOptions);
                if (result == null)
                {
                    System.Console.WriteLine("Error: Unable to parse response.");
                    return;
                }

                System.Console.WriteLine("Modules installed!");
                foreach (var name in result.InstalledModuleNames)
                    System.Console.WriteLine($"  + {name}");
                System.Console.WriteLine();
                System.Console.WriteLine($"  Interior: {result.InteriorSlotsUsed} used, {result.InteriorSlotsAvailable} available");
                System.Console.WriteLine($"  Exterior: {result.ExteriorSlotsUsed} used, {result.ExteriorSlotsAvailable} available");
            }, shipIdArg, moduleOption, jsonOption);

            return cmd;
        }

        private Command BuildUninstallCommand()
        {
            var shipIdArg = new Argument<Guid>("ship-id", "Ship ID");
            var moduleOption = new Option<Guid[]>("--module", "Installed module IDs to uninstall")
            {
                IsRequired = true,
                AllowMultipleArgumentsPerToken = true
            };
            var cargoOption = new Option<Guid>("--cargo", "Cargo hold module ID for boxed modules") { IsRequired = true };
            var jsonOption = new Option<bool>("--json", "Output as raw JSON");

            var cmd = new Command("uninstall", "Uninstall modules from ship")
            {
                shipIdArg, moduleOption, cargoOption, jsonOption
            };

            cmd.SetHandler(async (Guid shipId, Guid[] moduleIds, Guid cargoId, bool json) =>
            {
                var response = await _client.PostAsync($"/api/Ship/{shipId}/uninstall", new
                {
                    ModuleIds = moduleIds.ToList(),
                    CargoModuleId = cargoId
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

                var result = JsonSerializer.Deserialize<UninstallResponse>(content, JsonOptions);
                if (result == null)
                {
                    System.Console.WriteLine("Error: Unable to parse response.");
                    return;
                }

                System.Console.WriteLine("Modules uninstalled!");
                foreach (var name in result.UninstalledModuleNames)
                    System.Console.WriteLine($"  - {name}");
                System.Console.WriteLine();
                System.Console.WriteLine("  Boxed module IDs:");
                foreach (var id in result.BoxedModuleIds)
                    System.Console.WriteLine($"    {id}");
            }, shipIdArg, moduleOption, cargoOption, jsonOption);

            return cmd;
        }

        private static void FormatShipDetail(string json)
        {
            var ship = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);

            var name = ship.GetProperty("name").GetString() ?? "Unknown";
            var entityId = ship.GetProperty("entityId").GetString() ?? "";
            var shipClass = ship.TryGetProperty("class", out var cls) && cls.ValueKind != JsonValueKind.Null
                ? cls.GetString() ?? "" : "";
            var fleetId = ship.GetProperty("fleetId").GetString() ?? "";

            System.Console.WriteLine();
            System.Console.WriteLine($"=== {name} ===");
            System.Console.WriteLine($"  ID:    {entityId}");
            if (!string.IsNullOrEmpty(shipClass))
                System.Console.WriteLine($"  Class: {shipClass}");
            System.Console.WriteLine($"  Fleet: {fleetId}");

            // Structure and hull with max context
            if (ship.TryGetProperty("currentStructurePoints", out var sp) && sp.ValueKind != JsonValueKind.Null)
            {
                var spVal = sp.GetDecimal();
                var maxSp = ship.TryGetProperty("maxStructurePoints", out var msp) ? msp.GetDecimal() : 0;
                if (maxSp > 0)
                {
                    var spPct = spVal / maxSp * 100;
                    System.Console.WriteLine($"  Structure: {spVal:F0}/{maxSp:F0} ({spPct:F0}%)");
                }
                else
                    System.Console.WriteLine($"  Structure: {spVal:F0}");
            }
            if (ship.TryGetProperty("currentHullPoints", out var hp) && hp.ValueKind != JsonValueKind.Null)
            {
                var hpVal = hp.GetDecimal();
                var maxHp = ship.TryGetProperty("maxHullPoints", out var mhp) ? mhp.GetDecimal() : 0;
                if (maxHp > 0)
                {
                    var hpPct = hpVal / maxHp * 100;
                    System.Console.WriteLine($"  Hull:      {hpVal:F0}/{maxHp:F0} ({hpPct:F0}%)");
                }
                else
                    System.Console.WriteLine($"  Hull: {hpVal:F0}");
            }

            // Ship mass
            if (ship.TryGetProperty("shipMass", out var massEl) && massEl.GetDecimal() > 0)
                System.Console.WriteLine($"  Mass:      {massEl.GetDecimal():F0}");

            // Requirements met status
            if (ship.TryGetProperty("requirementsMet", out var reqMet) && reqMet.ValueKind != JsonValueKind.Null)
            {
                var met = reqMet.GetBoolean();
                System.Console.WriteLine($"  Requirements: {(met ? "Met" : "NOT MET")}");
            }

            // Aggregated capabilities
            if (ship.TryGetProperty("capabilities", out var caps) && caps.ValueKind == JsonValueKind.Array && caps.GetArrayLength() > 0)
            {
                System.Console.WriteLine();
                System.Console.WriteLine("Capabilities:");
                foreach (var cap in caps.EnumerateArray())
                {
                    var capType = cap.GetProperty("capabilityType").GetString() ?? "";
                    var capValue = cap.GetProperty("value").GetDecimal();
                    System.Console.WriteLine($"  {capType,-28} {capValue,10:F1}");
                }
            }

            // Aggregated requirements
            if (ship.TryGetProperty("requirements", out var reqs) && reqs.ValueKind == JsonValueKind.Array && reqs.GetArrayLength() > 0)
            {
                System.Console.WriteLine();
                System.Console.WriteLine("Requirements:");
                foreach (var req in reqs.EnumerateArray())
                {
                    var reqType = req.GetProperty("requirementType").GetString() ?? "";
                    var reqValue = req.GetProperty("value").GetDecimal();
                    System.Console.WriteLine($"  {reqType,-28} {reqValue,10:F1}");
                }
            }

            // Module list
            if (ship.TryGetProperty("modules", out var modules) && modules.ValueKind == JsonValueKind.Array && modules.GetArrayLength() > 0)
            {
                var interiorTotal = 0;
                var exteriorTotal = 0;

                System.Console.WriteLine();
                System.Console.WriteLine("Installed Modules:");
                System.Console.WriteLine($"  {"Name",-28} {"T",2} {"Category",-16} {"Slots",-8} {"Cond",5} {"Enabled",-7}");
                System.Console.WriteLine($"  {new string('-', 70)}");

                foreach (var mod in modules.EnumerateArray())
                {
                    var modName = mod.GetProperty("name").GetString() ?? "";
                    if (modName.Length > 26) modName = modName[..24] + "..";
                    var tier = mod.TryGetProperty("tier", out var t) ? t.GetInt32() : 0;
                    var category = mod.TryGetProperty("category", out var cat) && cat.ValueKind != JsonValueKind.Null
                        ? cat.GetString() ?? "" : "";
                    if (category.Length > 14) category = category[..12] + "..";
                    var intSlots = mod.GetProperty("interiorSlotsRequired").GetInt32();
                    var extSlots = mod.GetProperty("exteriorSlotsRequired").GetInt32();
                    var condition = mod.GetProperty("condition").GetDecimal();
                    var enabled = mod.GetProperty("isEnabled").GetBoolean();

                    interiorTotal += intSlots;
                    exteriorTotal += extSlots;

                    var slots = $"{intSlots}i/{extSlots}e";
                    System.Console.WriteLine($"  {modName,-28} {tier,2} {category,-16} {slots,-8} {condition,4:F0}% {(enabled ? "Yes" : "NO"),-7}");
                }

                var totalInt = ship.TryGetProperty("totalInteriorSlots", out var tiS) ? tiS.GetInt32() : 0;
                var totalExt = ship.TryGetProperty("totalExteriorSlots", out var teS) ? teS.GetInt32() : 0;

                System.Console.WriteLine();
                if (totalInt > 0 || totalExt > 0)
                    System.Console.WriteLine($"  Slots: {interiorTotal}/{totalInt} interior, {exteriorTotal}/{totalExt} exterior");
                else
                    System.Console.WriteLine($"  Slots used: {interiorTotal} interior, {exteriorTotal} exterior");
            }
            else
            {
                System.Console.WriteLine();
                System.Console.WriteLine("  (No module data available — you may not have permission to view this ship's internals)");
            }

            System.Console.WriteLine();
        }

        private static void PrintMaterializationResult(MaterializationResultResponse result)
        {
            System.Console.WriteLine($"  Job: {result.JobId.ToString()[..8]}...");
            System.Console.WriteLine($"  Resource: {result.ResourceName}");
            System.Console.WriteLine($"  Materialized: {result.MaterializedQuantity:F2} units");
            System.Console.WriteLine($"  Cargo ID: {result.BoxedResourceId}");
        }

        private class CargoItem
        {
            public Guid AssetId { get; set; }
            public string AssetType { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public decimal Quantity { get; set; }
            public decimal Mass { get; set; }
        }

        private class InstallResponse
        {
            public bool Success { get; set; }
            public string? ErrorMessage { get; set; }
            public List<string> InstalledModuleNames { get; set; } = new();
            public int InteriorSlotsUsed { get; set; }
            public int ExteriorSlotsUsed { get; set; }
            public int InteriorSlotsAvailable { get; set; }
            public int ExteriorSlotsAvailable { get; set; }
        }

        private class UninstallResponse
        {
            public bool Success { get; set; }
            public string? ErrorMessage { get; set; }
            public List<string> UninstalledModuleNames { get; set; } = new();
            public List<Guid> BoxedModuleIds { get; set; } = new();
        }
    }
}
