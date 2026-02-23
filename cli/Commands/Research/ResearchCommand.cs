using System.CommandLine;
using System.Text.Json;
using psecsapi.Console.Infrastructure.Configuration;
using psecsapi.Console.Infrastructure.Http;

namespace psecsapi.Console.Commands.Research
{
    public class ResearchCommand : ICommand
    {
        private readonly AuthenticatedHttpClient _client;
        private readonly CliConfig _config;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public ResearchCommand(AuthenticatedHttpClient client, CliConfig config)
        {
            _client = client;
            _config = config;
        }

        public Command Build()
        {
            var command = new Command("research", "Research management commands");

            command.AddCommand(BuildListCommand());
            command.AddCommand(BuildStatusCommand());
            command.AddCommand(BuildAllocateCommand());
            command.AddCommand(BuildStopCommand());
            command.AddCommand(BuildCompletedCommand());
            command.AddCommand(BuildModifiersCommand());
            command.AddCommand(BuildDisciplinesCommand());
            command.AddCommand(BuildComponentsCommand());
            command.AddCommand(BuildModulesCommand());

            return command;
        }

        private Command BuildListCommand()
        {
            var techOption = new Option<bool>("--technologies", () => true, "Include technologies");
            techOption.AddAlias("-t");
            var appOption = new Option<bool>("--applications", () => true, "Include applications");
            appOption.AddAlias("-a");
            var primaryOption = new Option<string?>("--primary", "Filter by primary discipline (B,C,E,I,M,P,S)");
            primaryOption.AddAlias("-p");
            var secondaryOption = new Option<string?>("--secondary", "Filter by secondary discipline");
            secondaryOption.AddAlias("-s");
            var tierOption = new Option<int?>("--tier", "Filter by tier level");
            var jsonOption = new Option<bool>("--json", "Output as JSON");

            var command = new Command("list", "List available research options")
            {
                techOption, appOption, primaryOption, secondaryOption, tierOption, jsonOption
            };

            command.SetHandler(async (technologies, applications, primary, secondary, tier, json) =>
            {
                var queryParams = new List<string>
                {
                    $"technologies={technologies.ToString().ToLower()}",
                    $"applications={applications.ToString().ToLower()}"
                };
                if (!string.IsNullOrEmpty(primary)) queryParams.Add($"primary={primary}");
                if (!string.IsNullOrEmpty(secondary)) queryParams.Add($"secondary={secondary}");
                if (tier.HasValue) queryParams.Add($"tier={tier}");

                var response = await _client.GetAsync($"/api/research/list?{string.Join("&", queryParams)}");
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
                    FormatListOutput(content);
                }
            }, techOption, appOption, primaryOption, secondaryOption, tierOption, jsonOption);

            return command;
        }

        private Command BuildStatusCommand()
        {
            var jsonOption = new Option<bool>("--json", "Output as JSON");

            var command = new Command("status", "Show current research status") { jsonOption };

            command.SetHandler(async (json) =>
            {
                var response = await _client.GetAsync("/api/research/status");
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
            }, jsonOption);

            return command;
        }

        private Command BuildAllocateCommand()
        {
            var targetArg = new Argument<string>("target-id", "Technology or application ID to research");
            var percentArg = new Argument<int>("percent", "Allocation percentage (1-100)");
            var jsonOption = new Option<bool>("--json", "Output as JSON");

            var command = new Command("allocate", "Start or update research allocation")
            {
                targetArg, percentArg, jsonOption
            };

            command.SetHandler(async (targetId, percent, json) =>
            {
                var requestBody = new { targetId, percent };
                var response = await _client.PostAsync("/api/research/allocate", requestBody);
                var content = await response.Content.ReadAsStringAsync();

                if (json)
                {
                    System.Console.WriteLine(content);
                }
                else
                {
                    FormatAllocateOutput(content, response.IsSuccessStatusCode);
                }
            }, targetArg, percentArg, jsonOption);

            return command;
        }

        private Command BuildStopCommand()
        {
            var targetArg = new Argument<string>("target-id", "Research target to stop");
            var jsonOption = new Option<bool>("--json", "Output as JSON");

            var command = new Command("stop", "Stop research on a target") { targetArg, jsonOption };

            command.SetHandler(async (targetId, json) =>
            {
                var requestBody = new { targetId };
                var response = await _client.PostAsync("/api/research/stop", requestBody);

                if (json)
                {
                    System.Console.WriteLine(JsonSerializer.Serialize(new { success = response.IsSuccessStatusCode }));
                }
                else
                {
                    if (response.IsSuccessStatusCode)
                    {
                        System.Console.WriteLine($"Stopped research on {targetId}. Progress retained.");
                    }
                    else
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        System.Console.WriteLine($"Error: {response.StatusCode}");
                        System.Console.WriteLine(content);
                    }
                }
            }, targetArg, jsonOption);

            return command;
        }

        private Command BuildCompletedCommand()
        {
            var techOption = new Option<bool>("--technologies", () => true, "Include technologies");
            techOption.AddAlias("-t");
            var appOption = new Option<bool>("--applications", () => true, "Include applications");
            appOption.AddAlias("-a");
            var jsonOption = new Option<bool>("--json", "Output as JSON");

            var command = new Command("completed", "List completed research")
            {
                techOption, appOption, jsonOption
            };

            command.SetHandler(async (technologies, applications, json) =>
            {
                var response = await _client.GetAsync(
                    $"/api/research/completed?technologies={technologies.ToString().ToLower()}&applications={applications.ToString().ToLower()}");
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
                    FormatCompletedOutput(content);
                }
            }, techOption, appOption, jsonOption);

            return command;
        }

        private Command BuildModifiersCommand()
        {
            var jsonOption = new Option<bool>("--json", "Output as JSON");

            var command = new Command("modifiers", "Show active research modifier bonuses") { jsonOption };

            command.SetHandler(async (json) =>
            {
                var response = await _client.GetAsync("/api/research/modifiers");
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    System.Console.WriteLine(content);
                    return;
                }

                if (json)
                {
                    System.Console.WriteLine(content);
                }
                else
                {
                    FormatModifiersOutput(content);
                }
            }, jsonOption);

            return command;
        }

        private Command BuildDisciplinesCommand()
        {
            var jsonOption = new Option<bool>("--json", "Output as JSON");
            var command = new Command("disciplines", "List tech tree disciplines") { jsonOption };

            command.SetHandler(async (json) =>
            {
                var response = await _client.GetAsync("/api/research/disciplines");
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    System.Console.WriteLine($"Error: {response.StatusCode}");
                    return;
                }

                if (json) { System.Console.WriteLine(content); return; }

                var doc = JsonDocument.Parse(content);
                System.Console.WriteLine();
                System.Console.WriteLine("Disciplines:");
                System.Console.WriteLine(new string('-', 60));
                foreach (var d in doc.RootElement.EnumerateArray())
                {
                    var code = d.GetProperty("code").GetString();
                    var name = d.GetProperty("name").GetString();
                    var desc = d.GetProperty("description").GetString();
                    System.Console.WriteLine($"  [{code}] {name,-15} {desc}");
                }
                System.Console.WriteLine();
            }, jsonOption);

            return command;
        }

        private Command BuildComponentsCommand()
        {
            var tierOption = new Option<int?>("--tier", "Filter by tier");
            var categoryOption = new Option<string?>("--category", "Filter by category");
            var jsonOption = new Option<bool>("--json", "Output as JSON");

            var command = new Command("components", "List tech tree components")
            {
                tierOption, categoryOption, jsonOption
            };

            command.SetHandler(async (tier, category, json) =>
            {
                var queryParams = new List<string>();
                if (tier.HasValue) queryParams.Add($"tier={tier}");
                if (!string.IsNullOrEmpty(category)) queryParams.Add($"category={category}");

                var url = "/api/research/components";
                if (queryParams.Count > 0) url += "?" + string.Join("&", queryParams);

                var response = await _client.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    System.Console.WriteLine($"Error: {response.StatusCode}");
                    return;
                }

                if (json) { System.Console.WriteLine(content); return; }

                var doc = JsonDocument.Parse(content);
                System.Console.WriteLine();
                System.Console.WriteLine("Components:");
                System.Console.WriteLine(new string('-', 100));
                System.Console.WriteLine($"  {"ID",-25} {"Name",-25} {"Category",-20} {"Tier",4} {"Mass",6}  Properties");
                System.Console.WriteLine(new string('-', 100));

                foreach (var c in doc.RootElement.EnumerateArray())
                {
                    var id = c.GetProperty("componentId").GetString();
                    var name = c.GetProperty("name").GetString();
                    var cat = c.GetProperty("category").GetString();
                    var t = c.GetProperty("tier").GetInt32();
                    var mass = c.TryGetProperty("mass", out var massEl) ? massEl.GetDecimal() : 0;

                    var props = new List<string>();
                    if (c.TryGetProperty("qualityProperties", out var propArr))
                    {
                        foreach (var p in propArr.EnumerateArray())
                            props.Add(p.GetString() ?? "");
                    }

                    System.Console.WriteLine($"  {id,-25} {name,-25} {cat,-20} {t,4} {mass,6}  {string.Join(", ", props)}");
                }
                System.Console.WriteLine();
            }, tierOption, categoryOption, jsonOption);

            return command;
        }

        private Command BuildModulesCommand()
        {
            var tierOption = new Option<int?>("--tier", "Filter by tier");
            var categoryOption = new Option<string?>("--category", "Filter by category");
            var jsonOption = new Option<bool>("--json", "Output as JSON");

            var command = new Command("modules", "List tech tree modules")
            {
                tierOption, categoryOption, jsonOption
            };

            command.SetHandler(async (tier, category, json) =>
            {
                var queryParams = new List<string>();
                if (tier.HasValue) queryParams.Add($"tier={tier}");
                if (!string.IsNullOrEmpty(category)) queryParams.Add($"category={category}");

                var url = "/api/research/modules";
                if (queryParams.Count > 0) url += "?" + string.Join("&", queryParams);

                var response = await _client.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    System.Console.WriteLine($"Error: {response.StatusCode}");
                    return;
                }

                if (json) { System.Console.WriteLine(content); return; }

                var doc = JsonDocument.Parse(content);
                System.Console.WriteLine();
                System.Console.WriteLine("Modules:");
                System.Console.WriteLine(new string('-', 130));
                System.Console.WriteLine($"  {"ID",-25} {"Name",-25} {"Category",-15} {"T",2} {"Mass",5} {"Slots",6} {"Requirements",-20} Capabilities");
                System.Console.WriteLine(new string('-', 130));

                foreach (var m in doc.RootElement.EnumerateArray())
                {
                    var id = m.GetProperty("moduleId").GetString();
                    var name = m.GetProperty("name").GetString();
                    var cat = m.GetProperty("category").GetString();
                    var t = m.GetProperty("tier").GetInt32();
                    var mass = m.GetProperty("mass").GetDecimal();

                    var intSlots = m.TryGetProperty("interiorSlotsRequired", out var iS) ? iS.GetInt32() : 0;
                    var extSlots = m.TryGetProperty("exteriorSlotsRequired", out var eS) ? eS.GetInt32() : 0;
                    var slotsStr = $"{intSlots}i/{extSlots}e";

                    var reqs = new List<string>();
                    if (m.TryGetProperty("requirements", out var reqArr))
                    {
                        foreach (var req in reqArr.EnumerateArray())
                        {
                            var reqType = req.GetProperty("type").GetString() ?? "";
                            var reqVal = req.GetProperty("value").GetDecimal();
                            var abbrev = reqType switch
                            {
                                "Power" => "Pwr",
                                "Compute" => "Cpu",
                                "Personnel" => "Crew",
                                _ => reqType
                            };
                            reqs.Add($"{abbrev}:{reqVal:G0}");
                        }
                    }
                    var reqStr = reqs.Count > 0 ? string.Join(" ", reqs) : "-";

                    var caps = new List<string>();
                    if (m.TryGetProperty("baseCapabilities", out var capArr))
                    {
                        foreach (var cap in capArr.EnumerateArray())
                        {
                            var type = cap.GetProperty("type").GetString();
                            var val = cap.GetProperty("baseValue").GetDecimal();
                            caps.Add($"{type}={val}");
                        }
                    }

                    System.Console.WriteLine($"  {id,-25} {name,-25} {cat,-15} {t,2} {mass,5} {slotsStr,6} {reqStr,-20} {string.Join(", ", caps)}");
                }
                System.Console.WriteLine();
            }, tierOption, categoryOption, jsonOption);

            return command;
        }

        private static void FormatListOutput(string json)
        {
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("technologies", out var techs) && techs.GetArrayLength() > 0)
            {
                System.Console.WriteLine();
                System.Console.WriteLine("Technologies:");
                System.Console.WriteLine(new string('-', 78));
                System.Console.WriteLine($"  {"Status",-8} {"ID",-8} {"Disc",5} {"Name",-30} {"Tier",4} {"Cost",8}");
                System.Console.WriteLine(new string('-', 78));

                foreach (var tech in techs.EnumerateArray())
                {
                    var status = tech.GetProperty("isResearched").GetBoolean() ? "[DONE]" :
                                 tech.GetProperty("isVisible").GetBoolean() ? "[AVAIL]" : "[LOCKED]";
                    var id = tech.GetProperty("id").GetString() ?? "";
                    var name = tech.GetProperty("name").GetString() ?? "";
                    var tier = tech.GetProperty("tier").GetInt32();
                    var cost = tech.GetProperty("researchCost").GetInt32();
                    var primary = tech.GetProperty("primaryDiscipline").GetString() ?? "";
                    var secondary = tech.GetProperty("secondaryDiscipline").GetString() ?? "";
                    var disc = primary == secondary ? primary : $"{primary}/{secondary}";

                    System.Console.WriteLine($"  {status,-8} {id,-8} {disc,5} {"",0}{name,-30} {tier,4} {cost,8}");
                }
            }

            if (root.TryGetProperty("applications", out var apps) && apps.GetArrayLength() > 0)
            {
                System.Console.WriteLine();
                System.Console.WriteLine("Applications:");
                System.Console.WriteLine(new string('-', 90));
                System.Console.WriteLine($"  {"Status",-8} {"ID",-10} {"Name",-30} {"Cost",8} {"Count",6}  Details");
                System.Console.WriteLine(new string('-', 90));

                foreach (var app in apps.EnumerateArray())
                {
                    var status = app.GetProperty("isResearched").GetBoolean() ? "[DONE]" :
                                 app.GetProperty("isVisible").GetBoolean() ? "[AVAIL]" : "[LOCKED]";
                    var id = app.GetProperty("id").GetString() ?? "";
                    var name = app.GetProperty("name").GetString() ?? "";
                    var cost = app.GetProperty("researchCost").GetInt32();
                    var count = app.GetProperty("instanceCount").GetInt32();
                    var countStr = count > 0 ? count.ToString() : "";

                    var details = "";
                    if (app.TryGetProperty("modifier", out var mod) && mod.ValueKind != JsonValueKind.Null)
                    {
                        var opType = mod.GetProperty("operationType").GetString();
                        var bonus = mod.GetProperty("bonusPercent").GetDecimal();
                        details = $"+{bonus}% {opType}";
                    }
                    else if (app.TryGetProperty("blueprint", out var bp) && bp.ValueKind != JsonValueKind.Null)
                    {
                        var outType = bp.GetProperty("outputType").GetString();
                        var outId = bp.GetProperty("outputId").GetString();
                        details = $"-> {outId} ({outType})";
                    }

                    System.Console.WriteLine($"  {status,-8} {id,-10} {name,-30} {cost,8} {countStr,6}  {details}");
                }
            }

            System.Console.WriteLine();
        }

        private static void FormatStatusOutput(string json)
        {
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var capacity = root.GetProperty("totalCapacity").GetInt32();
            var allocation = root.GetProperty("totalAllocation").GetInt32();

            System.Console.WriteLine();
            System.Console.WriteLine($"Research Capacity: {capacity} pts/hour");
            System.Console.WriteLine($"Total Allocation:  {allocation}%");
            System.Console.WriteLine();

            if (root.TryGetProperty("activeProjects", out var projects) && projects.GetArrayLength() > 0)
            {
                System.Console.WriteLine("Active Projects:");
                System.Console.WriteLine(new string('-', 80));

                foreach (var proj in projects.EnumerateArray())
                {
                    var id = proj.GetProperty("targetId").GetString() ?? "";
                    var name = proj.GetProperty("targetName").GetString() ?? "";
                    var current = proj.GetProperty("currentPoints").GetInt32();
                    var required = proj.GetProperty("requiredPoints").GetInt32();
                    var alloc = proj.GetProperty("allocationPercent").GetInt32();
                    var progress = required > 0 ? (current * 100.0 / required) : 0;

                    var bar = GenerateProgressBar(progress, 20);
                    System.Console.WriteLine($"  {id,-10} {name,-25} {bar} {current,6}/{required,-6}  {alloc}%");
                }
            }
            else
            {
                System.Console.WriteLine("No active research projects.");
            }

            System.Console.WriteLine();
        }

        private static void FormatAllocateOutput(string json, bool success)
        {
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!success || (root.TryGetProperty("success", out var successProp) && !successProp.GetBoolean()))
            {
                var error = root.TryGetProperty("errorMessage", out var msg) ? msg.GetString() : "Unknown error";
                System.Console.WriteLine($"Failed: {error}");
                return;
            }

            if (root.TryGetProperty("project", out var proj))
            {
                var id = proj.GetProperty("targetId").GetString();
                var name = proj.GetProperty("targetName").GetString();
                var alloc = proj.GetProperty("allocationPercent").GetInt32();
                var current = proj.GetProperty("currentPoints").GetInt32();
                var required = proj.GetProperty("requiredPoints").GetInt32();

                System.Console.WriteLine();
                System.Console.WriteLine($"Research started: {id} ({name})");
                System.Console.WriteLine($"Allocation: {alloc}%");
                System.Console.WriteLine($"Progress:   {current}/{required} points");
                System.Console.WriteLine();
            }
        }

        private static void FormatCompletedOutput(string json)
        {
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("technologies", out var techs) && techs.GetArrayLength() > 0)
            {
                System.Console.WriteLine();
                System.Console.WriteLine("Completed Technologies:");
                System.Console.WriteLine(new string('-', 30));
                foreach (var tech in techs.EnumerateArray())
                {
                    System.Console.WriteLine($"  {tech.GetString()}");
                }
            }

            if (root.TryGetProperty("applications", out var apps) && apps.GetArrayLength() > 0)
            {
                System.Console.WriteLine();
                System.Console.WriteLine("Completed Applications:");
                System.Console.WriteLine(new string('-', 60));
                System.Console.WriteLine($"  {"ID",-10} {"Name",-30} {"Quality",10}");
                System.Console.WriteLine(new string('-', 60));

                foreach (var app in apps.EnumerateArray())
                {
                    var id = app.GetProperty("applicationId").GetString() ?? "";
                    var name = app.GetProperty("name").GetString() ?? "";
                    var quality = app.GetProperty("quality").GetDecimal();
                    System.Console.WriteLine($"  {id,-10} {name,-30} {quality,10:P0}");
                }
            }

            if ((!root.TryGetProperty("technologies", out var t) || t.GetArrayLength() == 0) &&
                (!root.TryGetProperty("applications", out var a) || a.GetArrayLength() == 0))
            {
                System.Console.WriteLine();
                System.Console.WriteLine("No completed research yet.");
            }

            System.Console.WriteLine();
        }

        private static void FormatModifiersOutput(string json)
        {
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("modifiers", out var modifiers) &&
                modifiers.EnumerateObject().Any())
            {
                System.Console.WriteLine();
                System.Console.WriteLine("Active Research Modifiers:");
                System.Console.WriteLine(new string('-', 50));
                System.Console.WriteLine($"  {"Modifier",-35} {"Bonus",10}");
                System.Console.WriteLine(new string('-', 50));

                foreach (var mod in modifiers.EnumerateObject())
                {
                    var value = mod.Value.GetDecimal();
                    var sign = value >= 0 ? "+" : "";
                    System.Console.WriteLine($"  {mod.Name,-35} {sign}{value:F1}%");
                }
            }
            else
            {
                System.Console.WriteLine();
                System.Console.WriteLine("No active research modifiers.");
            }

            System.Console.WriteLine();
        }

        private static string GenerateProgressBar(double percent, int width)
        {
            var filled = (int)(percent / 100 * width);
            var empty = width - filled;
            return $"[{new string('#', filled)}{new string('-', empty)}]";
        }
    }
}
