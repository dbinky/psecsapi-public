using System.CommandLine;
using System.Text.Json;
using psecsapi.Console.Infrastructure.Configuration;
using psecsapi.Console.Infrastructure.Http;

namespace psecsapi.Console.Commands.Combat
{
    public class CombatCommand : ICommand
    {
        private readonly AuthenticatedHttpClient _client;
        private readonly CliConfig _config;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public CombatCommand(AuthenticatedHttpClient client, CliConfig config)
        {
            _client = client;
            _config = config;
        }

        public Command Build()
        {
            var command = new Command("combat", "Combat engagement, status, and script management");

            command.AddCommand(BuildEngageCommand());
            command.AddCommand(BuildStatusCommand());
            command.AddCommand(BuildSummaryCommand());
            command.AddCommand(BuildHistoryCommand());
            command.AddCommand(BuildReplayCommand());
            command.AddCommand(new CombatScriptSubCommand(_client, _config).Build());

            return command;
        }

        // ── combat engage ──────────────────────────────────────────────

        private Command BuildEngageCommand()
        {
            var attackerArg = new Argument<Guid>("attacker-fleet-id", "Your fleet ID (must be in same sector as target)");
            var targetArg = new Argument<Guid>("target-fleet-id", "Enemy fleet ID to attack");
            var jsonOption = new Option<bool>("--json", "Output as raw JSON");

            var cmd = new Command("engage", "Initiate fleet-vs-fleet combat (assign a script first!)") { attackerArg, targetArg, jsonOption };
            cmd.SetHandler(async (Guid attackerFleetId, Guid targetFleetId, bool json) =>
            {
                var response = await _client.PostAsync("/api/combat/engage", new
                {
                    AttackerFleetId = attackerFleetId,
                    TargetFleetId = targetFleetId
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

                var result = JsonSerializer.Deserialize<EngageResponse>(content, JsonOptions);
                if (result == null)
                {
                    System.Console.WriteLine("Error: Unable to parse response.");
                    return;
                }

                if (result.Success)
                {
                    System.Console.WriteLine("Combat engaged!");
                    System.Console.WriteLine($"  Combat ID: {result.CombatId}");
                    System.Console.WriteLine();
                    System.Console.WriteLine("Poll status with: papi combat status " + result.CombatId);
                }
                else
                {
                    System.Console.WriteLine($"Engagement failed: {result.ErrorMessage}");
                }
            }, attackerArg, targetArg, jsonOption);

            return cmd;
        }

        // ── combat status ──────────────────────────────────────────────

        private Command BuildStatusCommand()
        {
            var combatIdArg = new Argument<Guid>("combat-id", "Combat instance ID");
            var jsonOption = new Option<bool>("--json", "Output as raw JSON");

            var cmd = new Command("status", "Check current status of a combat instance") { combatIdArg, jsonOption };
            cmd.SetHandler(async (Guid combatId, bool json) =>
            {
                var response = await _client.GetAsync($"/api/combat/{combatId}/status");
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

                var result = JsonSerializer.Deserialize<StatusResponse>(content, JsonOptions);
                if (result == null)
                {
                    System.Console.WriteLine("Error: Unable to parse response.");
                    return;
                }

                System.Console.WriteLine($"Combat {result.CombatId}");
                System.Console.WriteLine($"  Status: {result.Status}");

                if (result.Status == "Completed")
                    System.Console.WriteLine($"\nGet results with: papi combat summary {result.CombatId}");
            }, combatIdArg, jsonOption);

            return cmd;
        }

        // ── combat summary ─────────────────────────────────────────────

        private Command BuildSummaryCommand()
        {
            var combatIdArg = new Argument<Guid>("combat-id", "Combat instance ID");
            var jsonOption = new Option<bool>("--json", "Output as raw JSON");

            var cmd = new Command("summary", "Get full summary of a completed combat") { combatIdArg, jsonOption };
            cmd.SetHandler(async (Guid combatId, bool json) =>
            {
                var response = await _client.GetAsync($"/api/combat/{combatId}/summary");
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

                var summary = JsonSerializer.Deserialize<SummaryResponse>(content, JsonOptions);
                if (summary == null)
                {
                    System.Console.WriteLine("Error: Unable to parse response.");
                    return;
                }

                System.Console.WriteLine();
                System.Console.WriteLine($"=== Combat Summary ===");
                System.Console.WriteLine($"  Combat ID:  {summary.CombatId}");
                System.Console.WriteLine($"  Outcome:    {summary.Outcome}");
                System.Console.WriteLine($"  Duration:   {summary.DurationSeconds:F1}s ({summary.DurationTicks} ticks)");
                System.Console.WriteLine($"  Timestamp:  {summary.Timestamp:yyyy-MM-dd HH:mm:ss} UTC");
                System.Console.WriteLine();
                System.Console.WriteLine($"  Attacker:   Corp {summary.AttackerCorpId}");
                System.Console.WriteLine($"              Fleet {summary.AttackerFleetId}");
                System.Console.WriteLine($"  Defender:   Corp {summary.DefenderCorpId}");
                System.Console.WriteLine($"              Fleet {summary.DefenderFleetId}");

                if (summary.ShipsDestroyed.Count > 0)
                {
                    System.Console.WriteLine();
                    System.Console.WriteLine("Ships Destroyed:");
                    foreach (var ship in summary.ShipsDestroyed)
                        System.Console.WriteLine($"    {ship}");
                }

                if (summary.ShipsFled.Count > 0)
                {
                    System.Console.WriteLine();
                    System.Console.WriteLine("Ships Fled:");
                    foreach (var ship in summary.ShipsFled)
                        System.Console.WriteLine($"    {ship}");
                }

                System.Console.WriteLine();
            }, combatIdArg, jsonOption);

            return cmd;
        }

        // ── combat history ─────────────────────────────────────────────

        private Command BuildHistoryCommand()
        {
            var pageOption = new Option<int?>("--page", "Page number (default: 1)");
            var pageSizeOption = new Option<int?>("--page-size", "Items per page (default: 20)");
            var jsonOption = new Option<bool>("--json", "Output as raw JSON");

            var cmd = new Command("history", "Get corp combat history") { pageOption, pageSizeOption, jsonOption };
            cmd.SetHandler(async (int? page, int? pageSize, bool json) =>
            {
                var corpId = _config.User.DefaultCorpId;
                if (!corpId.HasValue)
                {
                    System.Console.WriteLine("Error: No default corp ID set. Use 'papi config set-corp <id>'.");
                    return;
                }

                var url = $"/api/Corp/{corpId}/combat-history";
                var queryParams = new List<string>();
                if (page.HasValue) queryParams.Add($"page={page.Value}");
                if (pageSize.HasValue) queryParams.Add($"pageSize={pageSize.Value}");
                if (queryParams.Count > 0) url += "?" + string.Join("&", queryParams);

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

                var history = JsonSerializer.Deserialize<HistoryResponse>(content, JsonOptions);
                if (history == null)
                {
                    System.Console.WriteLine("Error: Unable to parse response.");
                    return;
                }

                System.Console.WriteLine();
                System.Console.WriteLine($"Combat History (page {history.Page}, {history.TotalCount} total)");
                System.Console.WriteLine();

                if (history.Items.Count == 0)
                {
                    System.Console.WriteLine("  No combat history.");
                    System.Console.WriteLine();
                    return;
                }

                System.Console.WriteLine($"  {"Date",-24} {"Outcome",-10} {"Opponent",-24} {"Kills",5} {"Losses",7}");
                System.Console.WriteLine($"  {new string('-', 74)}");

                foreach (var item in history.Items)
                {
                    var opName = item.OpponentCorpName.Length > 22
                        ? item.OpponentCorpName[..20] + ".."
                        : item.OpponentCorpName;
                    System.Console.WriteLine(
                        $"  {item.Timestamp:yyyy-MM-dd HH:mm:ss} UTC  {item.Outcome,-10} {opName,-24} {item.ShipKills,5} {item.ShipLosses,7}");
                }

                System.Console.WriteLine();
            }, pageOption, pageSizeOption, jsonOption);

            return cmd;
        }

        // ── combat replay ───────────────────────────────────────────────

        private Command BuildReplayCommand()
        {
            var combatIdArg = new Argument<Guid>("combat-id", "Combat instance ID");
            var outputOption = new Option<string?>("--output", "File path to save replay data (default: combat-<id>.replay)");
            outputOption.AddAlias("-o");

            var cmd = new Command("replay", "Download the replay binary for a completed combat (retained 90 days)")
                { combatIdArg, outputOption };

            cmd.SetHandler(async (Guid combatId, string? output) =>
            {
                var response = await _client.GetAsync($"/api/combat/{combatId}/replay");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Console.WriteLine($"Error: {response.StatusCode}");
                    System.Console.WriteLine(errorContent);
                    return;
                }

                var replayBytes = await response.Content.ReadAsByteArrayAsync();
                var filePath = Path.GetFullPath(output ?? $"combat-{combatId}.replay");
                await File.WriteAllBytesAsync(filePath, replayBytes);
                System.Console.WriteLine($"Replay saved: {filePath} ({replayBytes.Length:N0} bytes)");
            }, combatIdArg, outputOption);

            return cmd;
        }

        // ── Response models ────────────────────────────────────────────

        private class EngageResponse
        {
            public bool Success { get; set; }
            public Guid? CombatId { get; set; }
            public string? ErrorMessage { get; set; }
        }

        private class StatusResponse
        {
            public Guid CombatId { get; set; }
            public string Status { get; set; } = string.Empty;
        }

        private class SummaryResponse
        {
            public Guid CombatId { get; set; }
            public Guid AttackerCorpId { get; set; }
            public Guid DefenderCorpId { get; set; }
            public Guid AttackerFleetId { get; set; }
            public Guid DefenderFleetId { get; set; }
            public string Outcome { get; set; } = string.Empty;
            public int DurationTicks { get; set; }
            public double DurationSeconds { get; set; }
            public List<string> ShipsDestroyed { get; set; } = new();
            public List<string> ShipsFled { get; set; } = new();
            public DateTime Timestamp { get; set; }
        }

        private class HistoryResponse
        {
            public List<HistoryItem> Items { get; set; } = new();
            public int TotalCount { get; set; }
            public int Page { get; set; }
            public int PageSize { get; set; }
        }

        private class HistoryItem
        {
            public Guid CombatId { get; set; }
            public Guid OpponentCorpId { get; set; }
            public string OpponentCorpName { get; set; } = string.Empty;
            public string Outcome { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; }
            public int ShipLosses { get; set; }
            public int ShipKills { get; set; }
        }
    }
}
