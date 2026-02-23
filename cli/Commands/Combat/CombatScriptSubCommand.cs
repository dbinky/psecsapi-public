using System.CommandLine;
using System.Text.Json;
using psecsapi.Console.Infrastructure.Configuration;
using psecsapi.Console.Infrastructure.Http;

namespace psecsapi.Console.Commands.Combat;

public class CombatScriptSubCommand
{
    private readonly AuthenticatedHttpClient _client;
    private readonly CliConfig _config;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public CombatScriptSubCommand(AuthenticatedHttpClient client, CliConfig config)
    {
        _client = client;
        _config = config;
    }

    public Command Build()
    {
        var command = new Command("script", "Combat script management (create, edit, assign to fleets)");

        command.AddCommand(BuildScriptListCommand());
        command.AddCommand(BuildScriptGetCommand());
        command.AddCommand(BuildScriptCreateCommand());
        command.AddCommand(BuildScriptUpdateCommand());
        command.AddCommand(BuildScriptDeleteCommand());

        return command;
    }

    private Command BuildScriptListCommand()
    {
        var jsonOption = new Option<bool>("--json", "Output as raw JSON");

        var cmd = new Command("list", "List all combat scripts") { jsonOption };
        cmd.SetHandler(async (bool json) =>
        {
            var corpId = _config.User.DefaultCorpId;
            if (!corpId.HasValue)
            {
                System.Console.WriteLine("Error: No default corp ID set. Use 'papi config set-corp <id>'.");
                return;
            }

            var response = await _client.GetAsync($"/api/corp/{corpId}/scripts");
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

            var scripts = JsonSerializer.Deserialize<List<ScriptListItem>>(content, JsonOptions);
            if (scripts == null)
            {
                System.Console.WriteLine("Error: Unable to parse response.");
                return;
            }

            System.Console.WriteLine();
            if (scripts.Count == 0)
            {
                System.Console.WriteLine("No combat scripts. Create one with: papi combat script create --name \"My Script\" --file script.js");
                System.Console.WriteLine();
                return;
            }

            System.Console.WriteLine($"Combat Scripts ({scripts.Count}):");
            System.Console.WriteLine();
            System.Console.WriteLine($"  {"ID",-38} {"Name",-28} {"Modified",-20}");
            System.Console.WriteLine($"  {new string('-', 88)}");

            foreach (var s in scripts)
            {
                var name = s.Name.Length > 26 ? s.Name[..24] + ".." : s.Name;
                System.Console.WriteLine($"  {s.Id,-38} {name,-28} {s.Modified:yyyy-MM-dd HH:mm:ss} UTC");
            }

            System.Console.WriteLine();
        }, jsonOption);

        return cmd;
    }

    private Command BuildScriptGetCommand()
    {
        var scriptIdArg = new Argument<Guid>("script-id", "Combat script ID");
        var jsonOption = new Option<bool>("--json", "Output as raw JSON");

        var cmd = new Command("get", "Get a combat script including source code") { scriptIdArg, jsonOption };
        cmd.SetHandler(async (Guid scriptId, bool json) =>
        {
            var corpId = _config.User.DefaultCorpId;
            if (!corpId.HasValue)
            {
                System.Console.WriteLine("Error: No default corp ID set. Use 'papi config set-corp <id>'.");
                return;
            }

            var response = await _client.GetAsync($"/api/corp/{corpId}/scripts/{scriptId}");
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

            var script = JsonSerializer.Deserialize<ScriptDetail>(content, JsonOptions);
            if (script == null)
            {
                System.Console.WriteLine("Error: Unable to parse response.");
                return;
            }

            System.Console.WriteLine();
            System.Console.WriteLine($"=== {script.Name} ===");
            System.Console.WriteLine($"  ID:       {script.Id}");
            System.Console.WriteLine($"  Created:  {script.Created:yyyy-MM-dd HH:mm:ss} UTC");
            System.Console.WriteLine($"  Modified: {script.Modified:yyyy-MM-dd HH:mm:ss} UTC");
            System.Console.WriteLine();
            System.Console.WriteLine("Source:");
            System.Console.WriteLine(new string('-', 60));
            System.Console.WriteLine(script.Source);
            System.Console.WriteLine(new string('-', 60));
            System.Console.WriteLine();
        }, scriptIdArg, jsonOption);

        return cmd;
    }

    private Command BuildScriptCreateCommand()
    {
        var nameOption = new Option<string>("--name", "Script name") { IsRequired = true };
        nameOption.AddAlias("-n");
        var fileOption = new Option<string?>("--file", "Path to JavaScript file containing script source");
        fileOption.AddAlias("-f");
        var sourceOption = new Option<string?>("--source", "Inline script source (use --file for longer scripts)");
        sourceOption.AddAlias("-s");
        var jsonOption = new Option<bool>("--json", "Output as raw JSON");

        var cmd = new Command("create", "Create a new combat script from file or inline source") { nameOption, fileOption, sourceOption, jsonOption };
        cmd.SetHandler(async (string name, string? file, string? source, bool json) =>
        {
            var corpId = _config.User.DefaultCorpId;
            if (!corpId.HasValue)
            {
                System.Console.WriteLine("Error: No default corp ID set. Use 'papi config set-corp <id>'.");
                return;
            }

            var scriptSource = await ResolveScriptSource(file, source);
            if (scriptSource == null) return;

            var response = await _client.PostAsync($"/api/corp/{corpId}/scripts", new
            {
                Name = name,
                Source = scriptSource
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

            var script = JsonSerializer.Deserialize<ScriptDetail>(content, JsonOptions);
            if (script == null)
            {
                System.Console.WriteLine("Error: Unable to parse response.");
                return;
            }

            System.Console.WriteLine($"Script created: {script.Name} ({script.Id})");
            System.Console.WriteLine();
            System.Console.WriteLine($"Assign to a fleet with: papi fleet set-combat-script <fleet-id> --script-id {script.Id}");
        }, nameOption, fileOption, sourceOption, jsonOption);

        return cmd;
    }

    private Command BuildScriptUpdateCommand()
    {
        var scriptIdArg = new Argument<Guid>("script-id", "Combat script ID to update");
        var nameOption = new Option<string?>("--name", "Updated script name (optional — omit to keep current name)");
        nameOption.AddAlias("-n");
        var fileOption = new Option<string?>("--file", "Path to JavaScript file containing updated source");
        fileOption.AddAlias("-f");
        var sourceOption = new Option<string?>("--source", "Inline updated script source");
        sourceOption.AddAlias("-s");
        var jsonOption = new Option<bool>("--json", "Output as raw JSON");

        var cmd = new Command("update", "Update an existing combat script (partial updates supported — supply only what you want to change)") { scriptIdArg, nameOption, fileOption, sourceOption, jsonOption };
        cmd.SetHandler(async (Guid scriptId, string? name, string? file, string? source, bool json) =>
        {
            var corpId = _config.User.DefaultCorpId;
            if (!corpId.HasValue)
            {
                System.Console.WriteLine("Error: No default corp ID set. Use 'papi config set-corp <id>'.");
                return;
            }

            var scriptSource = await ResolveScriptSource(file, source, required: false);

            if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(scriptSource))
            {
                System.Console.WriteLine("Error: Provide at least --name, --file, or --source to update.");
                return;
            }

            // Fetch current script to fill in unchanged fields
            var getResponse = await _client.GetAsync($"/api/corp/{corpId}/scripts/{scriptId}");
            var getContent = await getResponse.Content.ReadAsStringAsync();

            if (!getResponse.IsSuccessStatusCode)
            {
                System.Console.WriteLine($"Error fetching current script: {getResponse.StatusCode}");
                System.Console.WriteLine(getContent);
                return;
            }

            var current = JsonSerializer.Deserialize<ScriptDetail>(getContent, JsonOptions);
            if (current == null)
            {
                System.Console.WriteLine("Error: Unable to parse current script.");
                return;
            }

            var response = await _client.PutAsync($"/api/corp/{corpId}/scripts/{scriptId}", new
            {
                Name = name ?? current.Name,
                Source = scriptSource ?? current.Source
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

            var script = JsonSerializer.Deserialize<ScriptDetail>(content, JsonOptions);
            if (script == null)
            {
                System.Console.WriteLine("Error: Unable to parse response.");
                return;
            }

            System.Console.WriteLine($"Script updated: {script.Name} ({script.Id})");
        }, scriptIdArg, nameOption, fileOption, sourceOption, jsonOption);

        return cmd;
    }

    private Command BuildScriptDeleteCommand()
    {
        var scriptIdArg = new Argument<Guid>("script-id", "Combat script ID to delete");
        var jsonOption = new Option<bool>("--json", "Output as raw JSON");

        var cmd = new Command("delete", "Delete a combat script") { scriptIdArg, jsonOption };
        cmd.SetHandler(async (Guid scriptId, bool json) =>
        {
            var corpId = _config.User.DefaultCorpId;
            if (!corpId.HasValue)
            {
                System.Console.WriteLine("Error: No default corp ID set. Use 'papi config set-corp <id>'.");
                return;
            }

            var response = await _client.DeleteAsync($"/api/corp/{corpId}/scripts/{scriptId}");

            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                System.Console.WriteLine($"Error: {response.StatusCode}");
                System.Console.WriteLine(content);
                return;
            }

            if (json)
            {
                System.Console.WriteLine("{}");
                return;
            }

            System.Console.WriteLine("Script deleted.");
        }, scriptIdArg, jsonOption);

        return cmd;
    }

    // ── Helpers ────────────────────────────────────────────────────

    private static async Task<string?> ResolveScriptSource(string? filePath, string? inlineSource, bool required = true)
    {
        if (!string.IsNullOrEmpty(filePath) && !string.IsNullOrEmpty(inlineSource))
        {
            System.Console.WriteLine("Error: Specify either --file or --source, not both.");
            return null;
        }

        if (!string.IsNullOrEmpty(filePath))
        {
            if (!File.Exists(filePath))
            {
                System.Console.WriteLine($"Error: File not found: {filePath}");
                return null;
            }
            return await File.ReadAllTextAsync(filePath);
        }

        if (!string.IsNullOrEmpty(inlineSource))
            return inlineSource;

        if (required)
        {
            System.Console.WriteLine("Error: Provide script source via --file <path> or --source <code>.");
            return null;
        }

        return null;
    }

    // ── Response models ────────────────────────────────────────────

    private class ScriptListItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
    }

    private class ScriptDetail
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
    }
}
