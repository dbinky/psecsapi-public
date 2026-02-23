using System.CommandLine;
using System.Text.Json;
using psecsapi.Console.Infrastructure.Configuration;
using psecsapi.Console.Infrastructure.Http;

namespace psecsapi.Console.Commands.Fleet
{
    public class FleetCommand : ICommand
    {
        private readonly AuthenticatedHttpClient _client;
        private readonly CliConfig _config;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public FleetCommand(AuthenticatedHttpClient client, CliConfig config)
        {
            _client = client;
            _config = config;
        }

        public Command Build()
        {
            var command = new Command("fleet", "Fleet management commands");

            command.AddCommand(BuildGetCommand());
            command.AddCommand(BuildScanCommand());
            command.AddCommand(BuildEnqueueCommand());
            command.AddCommand(BuildDequeueCommand());
            command.AddCommand(BuildSetCombatScriptCommand());
            command.AddCommand(BuildUnsetCombatScriptCommand());

            return command;
        }

        private Command BuildGetCommand()
        {
            var fleetIdArg = new Argument<Guid>("fleet-id", "Fleet ID");
            var jsonOption = new Option<bool>("--json", "Output as raw JSON");

            var command = new Command("get", "Get fleet details") { fleetIdArg, jsonOption };
            command.SetHandler(async (fleetId, json) =>
            {
                var response = await _client.GetAsync($"/api/Fleet/{fleetId}");
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

                FormatFleetDetail(content);
            }, fleetIdArg, jsonOption);

            return command;
        }

        private Command BuildScanCommand()
        {
            var command = new Command("scan", "Scanning commands for fleet reconnaissance");

            command.AddCommand(BuildScanSectorCommand());
            command.AddCommand(BuildScanDeepCommand());
            command.AddCommand(BuildScanSurveyCommand());
            command.AddCommand(BuildScanFleetCommand());

            return command;
        }

        private Command BuildScanSectorCommand()
        {
            var fleetIdArg = new Argument<Guid>("fleet-id", "Fleet ID");
            var jsonOption = new Option<bool>("--json", "Output as raw JSON");

            var command = new Command("sector", "Scan current sector (reveals sector info, conduits, orbitals)") { fleetIdArg, jsonOption };
            command.SetHandler(async (fleetId, json) =>
            {
                var response = await _client.GetAsync($"/api/Fleet/{fleetId}/scan");
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

                FormatSectorScan(content);
            }, fleetIdArg, jsonOption);

            return command;
        }

        private Command BuildScanDeepCommand()
        {
            var fleetIdArg = new Argument<Guid>("fleet-id", "Fleet ID");
            var orbitalOption = new Option<int?>("--orbital", "Orbital position for deep scan");
            var jsonOption = new Option<bool>("--json", "Output as raw JSON");

            var command = new Command("deep", "Deep scan for resource details") { fleetIdArg, orbitalOption, jsonOption };
            command.SetHandler(async (fleetId, orbital, json) =>
            {
                var url = $"/api/Fleet/{fleetId}/scan/deep";
                if (orbital.HasValue)
                {
                    url += $"?orbital={orbital.Value}";
                }
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
                    return;
                }

                FormatDeepScan(content);
            }, fleetIdArg, orbitalOption, jsonOption);

            return command;
        }

        private Command BuildScanSurveyCommand()
        {
            var fleetIdArg = new Argument<Guid>("fleet-id", "Fleet ID");
            var jsonOption = new Option<bool>("--json", "Output as raw JSON");

            var command = new Command("survey", "Survey all fleets in sector") { fleetIdArg, jsonOption };
            command.SetHandler(async (fleetId, json) =>
            {
                var response = await _client.GetAsync($"/api/Fleet/{fleetId}/survey");
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

                FormatSurvey(content);
            }, fleetIdArg, jsonOption);

            return command;
        }

        private Command BuildScanFleetCommand()
        {
            var fleetIdArg = new Argument<Guid>("fleet-id", "Fleet ID");
            var targetFleetIdArg = new Argument<Guid>("target-fleet-id", "Target fleet ID to scan");
            var jsonOption = new Option<bool>("--json", "Output as raw JSON");

            var command = new Command("fleet", "Detailed scan of a specific fleet") { fleetIdArg, targetFleetIdArg, jsonOption };
            command.SetHandler(async (fleetId, targetFleetId, json) =>
            {
                var response = await _client.GetAsync($"/api/Fleet/{fleetId}/scan/fleet/{targetFleetId}");
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

                FormatFleetScan(content);
            }, fleetIdArg, targetFleetIdArg, jsonOption);

            return command;
        }

        private Command BuildEnqueueCommand()
        {
            var fleetIdArg = new Argument<Guid>("fleet-id", "Fleet ID");
            var conduitIdOption = new Option<Guid>("--conduit", "Conduit ID") { IsRequired = true };
            conduitIdOption.AddAlias("-c");
            var jsonOption = new Option<bool>("--json", "Output as raw JSON");

            var command = new Command("enqueue", "Enqueue fleet at a conduit") { fleetIdArg, conduitIdOption, jsonOption };
            command.SetHandler(async (fleetId, conduitId, json) =>
            {
                var response = await _client.PutAsync($"/api/Fleet/{fleetId}/enqueue", new { conduitId });
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

                System.Console.WriteLine("Fleet enqueued!");
                System.Console.WriteLine();
                FormatFleetDetail(content);
            }, fleetIdArg, conduitIdOption, jsonOption);

            return command;
        }

        private Command BuildDequeueCommand()
        {
            var fleetIdArg = new Argument<Guid>("fleet-id", "Fleet ID");
            var jsonOption = new Option<bool>("--json", "Output as raw JSON");

            var command = new Command("dequeue", "Dequeue fleet from conduit") { fleetIdArg, jsonOption };
            command.SetHandler(async (fleetId, json) =>
            {
                var response = await _client.PutAsync($"/api/Fleet/{fleetId}/dequeue", null);
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

                System.Console.WriteLine("Fleet dequeued!");
                System.Console.WriteLine();
                FormatFleetDetail(content);
            }, fleetIdArg, jsonOption);

            return command;
        }

        private Command BuildSetCombatScriptCommand()
        {
            var fleetIdArg = new Argument<Guid>("fleet-id", "Fleet ID");
            var scriptIdOption = new Option<Guid>("--script-id", "Combat script ID to assign") { IsRequired = true };
            scriptIdOption.AddAlias("-s");

            var command = new Command("set-combat-script", "Assign a combat script to the fleet") { fleetIdArg, scriptIdOption };
            command.SetHandler(async (fleetId, scriptId) =>
            {
                var response = await _client.PutAsync($"/api/Fleet/{fleetId}/combat-script", new { scriptId });
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    System.Console.WriteLine($"Error: {response.StatusCode}");
                    System.Console.WriteLine(content);
                    return;
                }

                System.Console.WriteLine($"Combat script {scriptId} assigned to fleet {fleetId}.");
            }, fleetIdArg, scriptIdOption);

            return command;
        }

        private Command BuildUnsetCombatScriptCommand()
        {
            var fleetIdArg = new Argument<Guid>("fleet-id", "Fleet ID");

            var command = new Command("unset-combat-script", "Remove the combat script assignment from the fleet") { fleetIdArg };
            command.SetHandler(async (fleetId) =>
            {
                var response = await _client.DeleteAsync($"/api/Fleet/{fleetId}/combat-script");
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    System.Console.WriteLine($"Error: {response.StatusCode}");
                    System.Console.WriteLine(content);
                    return;
                }

                System.Console.WriteLine($"Combat script removed from fleet {fleetId}.");
            }, fleetIdArg);

            return command;
        }

        private static void FormatFleetDetail(string json)
        {
            var fleet = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);

            var name = fleet.GetProperty("name").GetString() ?? "Unknown";
            var entityId = fleet.GetProperty("entityId").GetString() ?? "";
            var status = fleet.TryGetProperty("status", out var st) && st.ValueKind != JsonValueKind.Null
                ? st.GetString() ?? "Unknown" : "Unknown";

            System.Console.WriteLine();
            System.Console.WriteLine($"=== {name} ===");
            System.Console.WriteLine($"  ID:      {entityId}");

            if (fleet.TryGetProperty("sectorId", out var sec) && sec.ValueKind != JsonValueKind.Null)
                System.Console.WriteLine($"  Sector:  {sec.GetString()}");

            System.Console.WriteLine($"  Status:  {status}");

            if (fleet.TryGetProperty("ships", out var ships) && ships.ValueKind == JsonValueKind.Array)
                System.Console.WriteLine($"  Ships:   {ships.GetArrayLength()}");

            // Queue status
            if (fleet.TryGetProperty("queueStatus", out var qs) && qs.ValueKind == JsonValueKind.Object)
            {
                System.Console.WriteLine();
                System.Console.WriteLine("Queue Status:");
                if (qs.TryGetProperty("conduitId", out var cid) && cid.ValueKind != JsonValueKind.Null)
                    System.Console.WriteLine($"  Conduit:  {cid.GetString()}");
                if (qs.TryGetProperty("queuePosition", out var qp))
                    System.Console.Write($"  Position: {qp.GetInt32()}");
                if (qs.TryGetProperty("queueWidth", out var qw))
                    System.Console.Write($"  (width: {qw.GetInt32()}");
                if (qs.TryGetProperty("queueLength", out var ql))
                    System.Console.Write($", depth: {ql.GetInt32()})");
                System.Console.WriteLine();
                if (qs.TryGetProperty("enqueuedTimestamp", out var et) && et.ValueKind != JsonValueKind.Null)
                    System.Console.WriteLine($"  Enqueued: {et.GetDateTime():yyyy-MM-dd HH:mm:ss}");
            }

            // Transit ETA
            if (fleet.TryGetProperty("transitETA", out var eta) && eta.ValueKind != JsonValueKind.Null)
            {
                System.Console.WriteLine();
                System.Console.WriteLine($"Transit ETA: {eta.GetDateTime():yyyy-MM-dd HH:mm:ss}");
            }

            System.Console.WriteLine();
        }

        private static void FormatSectorScan(string json)
        {
            var scan = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);

            var name = scan.GetProperty("name").GetString() ?? "Unknown";
            var entityId = scan.GetProperty("entityId").GetString() ?? "";
            var type = scan.TryGetProperty("type", out var t) && t.ValueKind != JsonValueKind.Null
                ? t.GetString() ?? "" : "";

            System.Console.WriteLine();
            System.Console.WriteLine($"=== {name} ({type}) ===");
            System.Console.WriteLine($"  ID: {entityId}");

            // Orbitals
            if (scan.TryGetProperty("orbitals", out var orbs) && orbs.ValueKind == JsonValueKind.Object)
            {
                System.Console.WriteLine();
                System.Console.WriteLine("Orbitals:");
                foreach (var orb in orbs.EnumerateObject())
                {
                    System.Console.WriteLine($"  [{orb.Name}] {orb.Value.GetString()}");
                }
            }

            // Conduits
            if (scan.TryGetProperty("conduits", out var conduits) && conduits.ValueKind == JsonValueKind.Array && conduits.GetArrayLength() > 0)
            {
                System.Console.WriteLine();
                System.Console.WriteLine("Conduits:");
                foreach (var c in conduits.EnumerateArray())
                {
                    var cId = c.GetProperty("entityId").GetString() ?? "";
                    var endpoint = c.TryGetProperty("endpointSectorId", out var ep) && ep.ValueKind != JsonValueKind.Null
                        ? ep.GetString() ?? "" : "?";
                    var length = c.TryGetProperty("length", out var ln) && ln.ValueKind != JsonValueKind.Null
                        ? ln.GetInt32().ToString() : "?";
                    var width = c.TryGetProperty("width", out var wd) && wd.ValueKind != JsonValueKind.Null
                        ? wd.GetInt32().ToString() : "?";
                    System.Console.WriteLine($"  {cId}");
                    System.Console.WriteLine($"    -> {endpoint}  length: {length}  width: {width}");
                }
            }
            else
            {
                System.Console.WriteLine();
                System.Console.WriteLine("  (No conduits detected)");
            }

            System.Console.WriteLine();
        }

        private static void FormatDeepScan(string json)
        {
            var arr = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);

            if (arr.ValueKind != JsonValueKind.Array)
            {
                System.Console.WriteLine("Unexpected response format.");
                return;
            }

            if (arr.GetArrayLength() == 0)
            {
                System.Console.WriteLine();
                System.Console.WriteLine("No resources detected at this location.");
                System.Console.WriteLine();
                return;
            }

            System.Console.WriteLine();
            System.Console.WriteLine($"Resources found: {arr.GetArrayLength()}");

            int index = 1;
            foreach (var res in arr.EnumerateArray())
            {
                var name = res.GetProperty("name").GetString() ?? "Unknown";
                var entityId = res.GetProperty("entityId").GetString() ?? "";
                var type = res.TryGetProperty("type", out var t) && t.ValueKind != JsonValueKind.Null
                    ? t.GetString() ?? "" : "";
                var cls = res.TryGetProperty("class", out var c) && c.ValueKind != JsonValueKind.Null
                    ? c.GetString() ?? "" : "";
                var order = res.TryGetProperty("order", out var o) && o.ValueKind != JsonValueKind.Null
                    ? o.GetString() ?? "" : "";

                System.Console.WriteLine();
                System.Console.WriteLine($"[{index++}] {name}");
                System.Console.WriteLine($"  ID:    {entityId}");
                if (!string.IsNullOrEmpty(type)) System.Console.WriteLine($"  Type:  {type}");
                if (!string.IsNullOrEmpty(cls)) System.Console.WriteLine($"  Class: {cls}");
                if (!string.IsNullOrEmpty(order)) System.Console.WriteLine($"  Order: {order}");

                // Property values with assessments
                var hasValues = res.TryGetProperty("propertyValues", out var vals) && vals.ValueKind == JsonValueKind.Object;
                var hasAssessments = res.TryGetProperty("propertyAssessments", out var assessments) && assessments.ValueKind == JsonValueKind.Object;

                if (hasValues)
                {
                    System.Console.WriteLine("  Properties:");
                    foreach (var prop in vals.EnumerateObject())
                    {
                        var value = prop.Value.GetInt32();
                        var assessment = "";
                        if (hasAssessments && assessments.TryGetProperty(prop.Name, out var a) && a.ValueKind != JsonValueKind.Null)
                            assessment = a.GetString() ?? "";

                        if (!string.IsNullOrEmpty(assessment))
                            System.Console.WriteLine($"    {prop.Name,-20} {value,5}  ({assessment})");
                        else
                            System.Console.WriteLine($"    {prop.Name,-20} {value,5}");
                    }
                }
            }

            System.Console.WriteLine();
        }

        private static void FormatSurvey(string json)
        {
            var survey = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);

            if (!survey.TryGetProperty("fleets", out var fleets) || fleets.ValueKind != JsonValueKind.Array)
            {
                System.Console.WriteLine("No fleet data in survey.");
                return;
            }

            System.Console.WriteLine();
            if (fleets.GetArrayLength() == 0)
            {
                System.Console.WriteLine("No other fleets detected in sector.");
                System.Console.WriteLine();
                return;
            }

            System.Console.WriteLine($"Fleets detected: {fleets.GetArrayLength()}");
            System.Console.WriteLine();
            System.Console.WriteLine($"  {"Name",-28} {"Ships",5}");
            System.Console.WriteLine($"  {new string('-', 35)}");

            foreach (var f in fleets.EnumerateArray())
            {
                var fName = f.GetProperty("name").GetString() ?? "Unknown";
                if (fName.Length > 26) fName = fName[..24] + "..";
                var ships = f.GetProperty("shipCount").GetInt32();
                System.Console.WriteLine($"  {fName,-28} {ships,5}");
            }

            System.Console.WriteLine();
        }

        private static void FormatFleetScan(string json)
        {
            var scan = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);

            var name = scan.GetProperty("name").GetString() ?? "Unknown";
            var fleetId = scan.GetProperty("fleetId").GetString() ?? "";
            var shipCount = scan.GetProperty("shipCount").GetInt32();

            System.Console.WriteLine();
            System.Console.WriteLine($"=== Fleet: {name} ===");
            System.Console.WriteLine($"  ID:    {fleetId}");
            System.Console.WriteLine($"  Ships: {shipCount}");

            if (scan.TryGetProperty("totalMass", out var mass) && mass.ValueKind != JsonValueKind.Null)
                System.Console.WriteLine($"  Mass:  {mass.GetDecimal():F1}");

            // Ships by class
            if (scan.TryGetProperty("shipsByClass", out var byClass) && byClass.ValueKind == JsonValueKind.Object)
            {
                System.Console.WriteLine();
                System.Console.WriteLine("Ships by Class:");
                foreach (var cls in byClass.EnumerateObject())
                {
                    System.Console.WriteLine($"  {cls.Name,-20} {cls.Value.GetInt32(),3}");
                }
            }

            // Individual ships
            if (scan.TryGetProperty("ships", out var ships) && ships.ValueKind == JsonValueKind.Array && ships.GetArrayLength() > 0)
            {
                System.Console.WriteLine();
                System.Console.WriteLine("Ships:");
                System.Console.WriteLine($"  {"Class",-16} {"Mass",8}  External Modules");
                System.Console.WriteLine($"  {new string('-', 60)}");

                foreach (var ship in ships.EnumerateArray())
                {
                    var shipClass = ship.TryGetProperty("class", out var sc) && sc.ValueKind != JsonValueKind.Null
                        ? sc.GetString() ?? "" : "";
                    if (shipClass.Length > 14) shipClass = shipClass[..12] + "..";
                    var shipMass = ship.TryGetProperty("mass", out var sm) ? sm.GetDecimal() : 0m;

                    var extModules = "";
                    if (ship.TryGetProperty("externalModules", out var ext) && ext.ValueKind == JsonValueKind.Array)
                    {
                        var moduleNames = new List<string>();
                        foreach (var mod in ext.EnumerateArray())
                        {
                            var modName = mod.GetProperty("name").GetString() ?? "";
                            moduleNames.Add(modName);
                        }
                        extModules = moduleNames.Count > 0 ? string.Join(", ", moduleNames) : "(none visible)";
                    }

                    System.Console.WriteLine($"  {shipClass,-16} {shipMass,8:F1}  {extModules}");
                }
            }

            System.Console.WriteLine();
        }
    }
}
