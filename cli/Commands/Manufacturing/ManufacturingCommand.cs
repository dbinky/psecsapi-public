using System.CommandLine;
using System.Text.Json;
using psecsapi.Console.Infrastructure.Configuration;
using psecsapi.Console.Infrastructure.Http;

namespace psecsapi.Console.Commands.Manufacturing;

public class ManufacturingCommand : ICommand
{
    private readonly AuthenticatedHttpClient _client;
    private readonly CliConfig _config;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ManufacturingCommand(AuthenticatedHttpClient client, CliConfig config)
    {
        _client = client;
        _config = config;
    }

    public Command Build()
    {
        var command = new Command("manufacturing", "Manufacturing and blueprint commands");

        command.AddCommand(BuildBlueprintsCommand());
        command.AddCommand(BuildBlueprintDetailCommand());
        command.AddCommand(BuildStatusCommand());
        command.AddCommand(BuildStartCommand());
        command.AddCommand(BuildPauseCommand());
        command.AddCommand(BuildResumeCommand());
        command.AddCommand(BuildCancelCommand());

        return command;
    }

    private Command BuildBlueprintsCommand()
    {
        var jsonOption = new Option<bool>("--json", "Output as JSON");

        var command = new Command("blueprints", "List owned blueprints") { jsonOption };

        command.SetHandler(async (json) =>
        {
            var response = await _client.GetAsync("/api/manufacturing/blueprints");
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
            }
            else
            {
                FormatBlueprintsOutput(content);
            }
        }, jsonOption);

        return command;
    }

    private Command BuildBlueprintDetailCommand()
    {
        var blueprintArg = new Argument<string>("blueprint-id", "Blueprint definition ID to inspect");
        var jsonOption = new Option<bool>("--json", "Output as JSON");

        var command = new Command("blueprint", "View blueprint details")
        {
            blueprintArg, jsonOption
        };

        command.SetHandler(async (blueprintId, json) =>
        {
            var response = await _client.GetAsync($"/api/manufacturing/blueprint/{blueprintId}");
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
            }
            else
            {
                FormatBlueprintDetailOutput(content);
            }
        }, blueprintArg, jsonOption);

        return command;
    }

    private Command BuildStatusCommand()
    {
        var shipOption = new Option<Guid?>("--ship", "Filter by ship ID");
        var jsonOption = new Option<bool>("--json", "Output as JSON");

        var command = new Command("status", "Show manufacturing job status")
        {
            shipOption, jsonOption
        };

        command.SetHandler(async (shipId, json) =>
        {
            var url = "/api/manufacturing/status";
            if (shipId.HasValue) url += $"?shipId={shipId}";

            var response = await _client.GetAsync(url);
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
            }
            else
            {
                FormatStatusOutput(content);
            }
        }, shipOption, jsonOption);

        return command;
    }

    private Command BuildStartCommand()
    {
        var shipArg = new Argument<Guid>("ship-id", "Ship to manufacture on");
        var blueprintArg = new Argument<Guid>("blueprint-instance-id", "Blueprint instance to use");
        var quantityOption = new Option<int>("--quantity", () => 1, "Number of items to produce");
        var nameOption = new Option<string?>("--name", "Display name for the output");
        var autoResumeOption = new Option<bool>("--auto-resume", "Auto-resume when resources become available");
        var inputOption = new Option<string[]>("--input", "Input resources/components (label:boxed-asset-id)")
        {
            AllowMultipleArgumentsPerToken = true
        };
        var jsonOption = new Option<bool>("--json", "Output as JSON");

        var command = new Command("start", "Start a manufacturing job")
        {
            shipArg, blueprintArg, quantityOption, nameOption, autoResumeOption, inputOption, jsonOption
        };

        command.SetHandler(async (shipId, blueprintInstanceId, quantity, displayName, autoResume, inputs, json) =>
        {
            Dictionary<string, List<Guid>>? selectedInputs = null;
            if (inputs != null && inputs.Length > 0)
            {
                selectedInputs = new Dictionary<string, List<Guid>>();
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
            }

            var body = new
            {
                shipId,
                blueprintInstanceId,
                quantity,
                displayName,
                autoResume,
                inputs = selectedInputs
            };
            var response = await _client.PostAsync("/api/manufacturing/start", body);
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

            var result = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
            var jobId = result.GetProperty("jobId").GetString() ?? "";
            var status = result.GetProperty("status").GetString() ?? "";

            System.Console.WriteLine("Manufacturing job started!");
            System.Console.WriteLine($"  Job ID: {jobId}");
            System.Console.WriteLine($"  Status: {status}");
            if (result.TryGetProperty("nextTickAt", out var nextTick) && nextTick.ValueKind != JsonValueKind.Null)
            {
                var tickTime = nextTick.GetDateTime();
                var remaining = tickTime - DateTime.UtcNow;
                System.Console.WriteLine($"  First tick: ~{remaining.TotalMinutes:F0}m");
            }
        }, shipArg, blueprintArg, quantityOption, nameOption, autoResumeOption, inputOption, jsonOption);

        return command;
    }

    private Command BuildPauseCommand()
    {
        var jobArg = new Argument<Guid>("job-id", "Job ID to pause");

        var command = new Command("pause", "Pause a manufacturing job") { jobArg };

        command.SetHandler(async (jobId) =>
        {
            var body = new { jobId };
            var response = await _client.PostAsync("/api/manufacturing/pause", body);

            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                System.Console.WriteLine($"Error: {response.StatusCode}");
                System.Console.WriteLine(content);
                return;
            }

            System.Console.WriteLine("Job paused.");
        }, jobArg);

        return command;
    }

    private Command BuildResumeCommand()
    {
        var jobArg = new Argument<Guid>("job-id", "Job ID to resume");

        var command = new Command("resume", "Resume a paused manufacturing job") { jobArg };

        command.SetHandler(async (jobId) =>
        {
            var body = new { jobId };
            var response = await _client.PostAsync("/api/manufacturing/resume", body);

            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                System.Console.WriteLine($"Error: {response.StatusCode}");
                System.Console.WriteLine(content);
                return;
            }

            System.Console.WriteLine("Job resumed.");
        }, jobArg);

        return command;
    }

    private Command BuildCancelCommand()
    {
        var jobArg = new Argument<Guid>("job-id", "Job ID to cancel");

        var command = new Command("cancel", "Cancel a manufacturing job") { jobArg };

        command.SetHandler(async (jobId) =>
        {
            var body = new { jobId };
            var response = await _client.PostAsync("/api/manufacturing/cancel", body);

            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                System.Console.WriteLine($"Error: {response.StatusCode}");
                System.Console.WriteLine(content);
                return;
            }

            System.Console.WriteLine("Job cancelled.");
        }, jobArg);

        return command;
    }

    private static void FormatBlueprintsOutput(string json)
    {
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.GetArrayLength() == 0)
        {
            System.Console.WriteLine();
            System.Console.WriteLine("No blueprints owned. Research blueprint applications to acquire them.");
            System.Console.WriteLine();
            return;
        }

        System.Console.WriteLine();
        System.Console.WriteLine("Owned Blueprints:");
        System.Console.WriteLine(new string('-', 90));
        System.Console.WriteLine($"  {"Instance ID",-38} {"Output",-25} {"Type",-12} {"Quality",10}");
        System.Console.WriteLine(new string('-', 90));

        foreach (var bp in root.EnumerateArray())
        {
            var instanceId = bp.GetProperty("instanceId").GetString() ?? "";
            var outputName = bp.GetProperty("outputName").GetString() ?? "";
            var outputType = bp.GetProperty("outputType").GetString() ?? "";
            var quality = bp.GetProperty("quality").GetDecimal();

            System.Console.WriteLine($"  {instanceId,-38} {outputName,-25} {outputType,-12} {quality,10:P0}");
        }

        System.Console.WriteLine();
    }

    private static void FormatBlueprintDetailOutput(string json)
    {
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var blueprintId = root.GetProperty("blueprintId").GetString();
        var outputType = root.GetProperty("outputType").GetString();
        var outputId = root.GetProperty("outputId").GetString();
        var baseWorkUnits = root.GetProperty("baseWorkUnits").GetInt32();

        System.Console.WriteLine();
        System.Console.WriteLine($"Blueprint: {blueprintId}");
        System.Console.WriteLine($"Produces:  {outputId} ({outputType})");
        System.Console.WriteLine($"Work:      {baseWorkUnits} base work units");

        if (root.TryGetProperty("inputResources", out var resources) && resources.GetArrayLength() > 0)
        {
            System.Console.WriteLine();
            System.Console.WriteLine("Resource Inputs:");
            foreach (var res in resources.EnumerateArray())
            {
                var label = res.GetProperty("label").GetString();
                var qualifier = res.GetProperty("qualifier").GetString();
                var value = res.GetProperty("value").GetString();
                var quantity = res.GetProperty("quantity").GetInt32();

                var inputKind = res.TryGetProperty("inputKind", out var ik) && ik.ValueKind != JsonValueKind.Null
                    ? ik.GetString() : null;
                var alloyDefId = res.TryGetProperty("alloyDefinitionId", out var ad) && ad.ValueKind != JsonValueKind.Null
                    ? ad.GetString() : null;
                var role = res.TryGetProperty("role", out var r) && r.ValueKind != JsonValueKind.Null
                    ? r.GetString() : null;

                if (inputKind == "Alloy" && alloyDefId != null)
                {
                    var roleSuffix = role != null ? $" [{role}]" : "";
                    System.Console.WriteLine($"  {label}: {quantity}x Alloy:{alloyDefId}{roleSuffix}");
                }
                else
                {
                    var roleSuffix = role != null ? $" [{role}]" : "";
                    System.Console.WriteLine($"  {label}: {quantity}x ({qualifier}={value}){roleSuffix}");
                }
            }
        }

        if (root.TryGetProperty("inputComponents", out var components) && components.GetArrayLength() > 0)
        {
            System.Console.WriteLine();
            System.Console.WriteLine("Component Inputs:");
            foreach (var comp in components.EnumerateArray())
            {
                var label = comp.GetProperty("label").GetString();
                var compType = comp.GetProperty("componentType").GetString();
                var quantity = comp.GetProperty("quantity").GetInt32();
                System.Console.WriteLine($"  {label}: {quantity}x {compType}");
            }
        }

        if (root.TryGetProperty("capabilities", out var caps) && caps.ValueKind == JsonValueKind.Array && caps.GetArrayLength() > 0)
        {
            System.Console.WriteLine();
            System.Console.WriteLine("Output Capabilities:");
            foreach (var cap in caps.EnumerateArray())
            {
                var type = cap.GetProperty("type").GetString();
                var baseValue = cap.GetProperty("baseValue").GetDecimal();
                var qualitySource = cap.TryGetProperty("qualitySource", out var qs) && qs.ValueKind != JsonValueKind.Null
                    ? qs.GetString() : null;
                var qualitySuffix = qualitySource != null ? $" (scaled by {qualitySource})" : "";
                System.Console.WriteLine($"  {type}: {baseValue}{qualitySuffix}");
            }
        }

        System.Console.WriteLine();
    }

    private static void FormatStatusOutput(string json)
    {
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("jobs", out var jobs) || jobs.GetArrayLength() == 0)
        {
            System.Console.WriteLine();
            System.Console.WriteLine("No active manufacturing jobs.");
            System.Console.WriteLine();
            return;
        }

        var totalActive = root.TryGetProperty("totalActive", out var ta) ? ta.GetInt32() : 0;
        var totalPaused = root.TryGetProperty("totalPaused", out var tp) ? tp.GetInt32() : 0;

        System.Console.WriteLine();
        System.Console.WriteLine($"Manufacturing Jobs ({totalActive} active, {totalPaused} paused/waiting):");
        System.Console.WriteLine(new string('-', 116));
        System.Console.WriteLine($"  {"Job ID",-38} {"Ship",-16} {"Output",-20} {"Progress",-15} {"Status",-12} {"ETA"}");
        System.Console.WriteLine(new string('-', 116));

        foreach (var job in jobs.EnumerateArray())
        {
            var jobId = job.GetProperty("jobId").GetString() ?? "";
            var shipName = job.TryGetProperty("shipName", out var sn) && sn.ValueKind != JsonValueKind.Null
                ? sn.GetString() ?? "" : "";
            if (shipName.Length > 14) shipName = shipName[..14] + "..";

            var displayName = job.TryGetProperty("displayName", out var dn) && dn.ValueKind != JsonValueKind.Null
                ? dn.GetString() ?? "" : "";
            var outputName = job.TryGetProperty("outputName", out var on) && on.ValueKind != JsonValueKind.Null
                ? on.GetString() ?? "" : "";
            var blueprintId = job.TryGetProperty("blueprintId", out var bp) && bp.ValueKind != JsonValueKind.Null
                ? bp.GetString() ?? "" : "";
            var output = !string.IsNullOrEmpty(displayName) ? displayName
                : !string.IsNullOrEmpty(outputName) ? outputName
                : blueprintId;
            if (output.Length > 18) output = output[..18] + "..";

            var completed = job.GetProperty("completedCount").GetInt32();
            var target = job.GetProperty("targetQuantity").GetInt32();
            var progressPct = job.TryGetProperty("currentItemProgressPercent", out var pp) ? pp.GetInt32() : 0;
            var progress = $"{completed}/{target} ({progressPct}%)";

            var status = job.GetProperty("status").GetString() ?? "";

            var eta = "";
            if (job.TryGetProperty("estimatedCompletion", out var ec) && ec.ValueKind != JsonValueKind.Null)
            {
                var estTime = ec.GetDateTime();
                var remaining = estTime - DateTime.UtcNow;
                if (remaining.TotalMinutes > 0)
                    eta = $"~{remaining.TotalMinutes:F0}m";
                else
                    eta = "soon";
            }

            System.Console.WriteLine($"  {jobId,-38} {shipName,-16} {output,-20} {progress,-15} {status,-12} {eta}");
        }

        System.Console.WriteLine();
    }
}
