using System.CommandLine;
using System.Text.Json;
using psecsapi.Console.Infrastructure.Configuration;
using psecsapi.Console.Infrastructure.Http;

namespace psecsapi.Console.Commands.Corp
{
    public class CorpCommand : ICommand
    {
        private readonly AuthenticatedHttpClient _client;
        private readonly CliConfig _config;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public CorpCommand(AuthenticatedHttpClient client, CliConfig config)
        {
            _client = client;
            _config = config;
        }

        public Command Build()
        {
            var command = new Command("corp", "Corporation management commands");

            command.AddCommand(BuildGetCommand());
            command.AddCommand(BuildFleetsCommand());
            command.AddCommand(BuildEventsCommand());

            return command;
        }

        private Command BuildGetCommand()
        {
            var corpIdOption = new Option<Guid?>("--id", "Corporation ID (uses default if not specified)");
            corpIdOption.AddAlias("-i");
            var jsonOption = new Option<bool>("--json", "Output as raw JSON");

            var command = new Command("get", "Get corporation details") { corpIdOption, jsonOption };
            command.SetHandler(async (corpId, json) =>
            {
                var id = corpId ?? _config.User.DefaultCorpId;
                if (!id.HasValue)
                {
                    System.Console.WriteLine("Error: No corp ID specified and no default corp ID set.");
                    return;
                }

                var response = await _client.GetAsync($"/api/Corp/{id}");
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

                FormatCorpDetail(content);
            }, corpIdOption, jsonOption);

            return command;
        }

        private Command BuildFleetsCommand()
        {
            var corpIdOption = new Option<Guid?>("--id", "Corporation ID (uses default if not specified)");
            corpIdOption.AddAlias("-i");
            var jsonOption = new Option<bool>("--json", "Output as raw JSON");

            var command = new Command("fleets", "Get corporation fleets") { corpIdOption, jsonOption };
            command.SetHandler(async (corpId, json) =>
            {
                var id = corpId ?? _config.User.DefaultCorpId;
                if (!id.HasValue)
                {
                    System.Console.WriteLine("Error: No corp ID specified and no default corp ID set.");
                    return;
                }

                var response = await _client.GetAsync($"/api/Corp/{id}/Fleets");
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

                FormatCorpFleets(content);
            }, corpIdOption, jsonOption);

            return command;
        }

        private static void FormatCorpDetail(string json)
        {
            var corp = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);

            var entityId = corp.GetProperty("entityId").GetString() ?? "";
            var corpName = corp.GetProperty("name").GetString() ?? "Unknown";

            System.Console.WriteLine();
            System.Console.WriteLine($"=== {corpName} ===");
            System.Console.WriteLine($"  ID:      {entityId}");
            System.Console.WriteLine($"  Created: {corp.GetProperty("createTimestamp").GetDateTime():yyyy-MM-dd HH:mm:ss}");

            if (corp.TryGetProperty("lastUpdateTimestamp", out var lut) && lut.ValueKind != JsonValueKind.Null)
                System.Console.WriteLine($"  Updated: {lut.GetDateTime():yyyy-MM-dd HH:mm:ss}");

            if (corp.TryGetProperty("credits", out var credits) && credits.ValueKind != JsonValueKind.Null)
                System.Console.WriteLine($"  Credits: {credits.GetDecimal():N2}");

            // Access levels (members and their roles)
            if (corp.TryGetProperty("accessLevels", out var access) && access.ValueKind == JsonValueKind.Object)
            {
                var members = access.EnumerateObject().ToList();
                if (members.Count > 0)
                {
                    System.Console.WriteLine();
                    System.Console.WriteLine("Members:");
                    foreach (var member in members)
                    {
                        var userId = member.Name;
                        var role = member.Value.ValueKind == JsonValueKind.Number
                            ? GetRoleName(member.Value.GetInt32())
                            : member.Value.GetString() ?? "Unknown";
                        System.Console.WriteLine($"  {userId}  ({role})");
                    }
                }
            }

            System.Console.WriteLine();
        }

        private static void FormatCorpFleets(string json)
        {
            var data = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);

            if (!data.TryGetProperty("corpFleets", out var fleets) || fleets.ValueKind != JsonValueKind.Array)
            {
                System.Console.WriteLine("No fleet data.");
                return;
            }

            System.Console.WriteLine();
            if (fleets.GetArrayLength() == 0)
            {
                System.Console.WriteLine("No fleets owned by this corporation.");
                System.Console.WriteLine();
                return;
            }

            System.Console.WriteLine($"Fleets: {fleets.GetArrayLength()}");
            System.Console.WriteLine();

            var index = 1;
            foreach (var fleet in fleets.EnumerateArray())
            {
                System.Console.WriteLine($"  [{index}] {fleet.GetString()}");
                index++;
            }

            System.Console.WriteLine();
            System.Console.WriteLine("Use 'fleet get <fleet-id>' for fleet details.");
            System.Console.WriteLine();
        }

        private static string GetRoleName(int level) => level switch
        {
            0 => "Anonymous",
            1 => "Member",
            2 => "Officer",
            3 => "Owner",
            _ => $"Level {level}"
        };

        private Command BuildEventsCommand()
        {
            var sinceOption = new Option<string?>("--since", "Return events after this timestamp (ISO 8601, e.g. 2024-01-15T14:00:00Z)");
            var untilOption = new Option<string?>("--until", "Return events before this timestamp (ISO 8601)");
            var typeOption = new Option<string?>("--type", "Filter by event type (e.g. com.psecsapi.fleet.moved)");
            var sourceOption = new Option<string?>("--source", "Filter by source prefix");
            var limitOption = new Option<int?>("--limit", "Maximum events to return (1-1000, default 100)");
            var cursorOption = new Option<string?>("--cursor", "Pagination cursor from a previous response");
            var jsonOption = new Option<bool>("--json", "Output as raw JSON");

            var command = new Command("events", "Query corp event log (mining, market, combat, manufacturing events)")
            {
                sinceOption, untilOption, typeOption, sourceOption, limitOption, cursorOption, jsonOption
            };

            command.SetHandler(async (string? since, string? until, string? type, string? source, int? limit, string? cursor, bool json) =>
            {
                var corpId = _config.User.DefaultCorpId;
                if (!corpId.HasValue)
                {
                    System.Console.WriteLine("Error: No default corp ID set. Use 'papi config set-corp <id>'.");
                    return;
                }

                var url = $"/api/corp/{corpId}/events";
                var queryParams = new List<string>();
                if (!string.IsNullOrEmpty(since)) queryParams.Add($"since={Uri.EscapeDataString(since)}");
                if (!string.IsNullOrEmpty(until)) queryParams.Add($"until={Uri.EscapeDataString(until)}");
                if (!string.IsNullOrEmpty(type)) queryParams.Add($"type={Uri.EscapeDataString(type)}");
                if (!string.IsNullOrEmpty(source)) queryParams.Add($"source={Uri.EscapeDataString(source)}");
                if (limit.HasValue) queryParams.Add($"limit={limit.Value}");
                if (!string.IsNullOrEmpty(cursor)) queryParams.Add($"cursor={Uri.EscapeDataString(cursor)}");
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

                var result = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);

                if (!result.TryGetProperty("events", out var events) || events.ValueKind != JsonValueKind.Array)
                {
                    System.Console.WriteLine("Error: Unable to parse response.");
                    return;
                }

                var eventCount = events.GetArrayLength();
                System.Console.WriteLine();

                if (eventCount == 0)
                {
                    System.Console.WriteLine("No events found.");
                    System.Console.WriteLine();
                    return;
                }

                System.Console.WriteLine($"Events ({eventCount}):");
                System.Console.WriteLine();

                foreach (var evt in events.EnumerateArray())
                {
                    var time = evt.TryGetProperty("time", out var t) && t.ValueKind != JsonValueKind.Null
                        ? t.GetDateTime().ToString("yyyy-MM-dd HH:mm:ss") + " UTC"
                        : "(no time)";
                    var evtType = evt.TryGetProperty("type", out var tp) ? tp.GetString() ?? "" : "";
                    var evtSource = evt.TryGetProperty("source", out var s) ? s.GetString() ?? "" : "";
                    var evtId = evt.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "";

                    System.Console.WriteLine($"  [{time}] {evtType}");
                    System.Console.WriteLine($"    ID:     {evtId}");
                    System.Console.WriteLine($"    Source: {evtSource}");

                    if (evt.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
                    {
                        var dataProps = data.EnumerateObject().ToList();
                        if (dataProps.Count > 0)
                        {
                            System.Console.WriteLine("    Data:");
                            foreach (var prop in dataProps)
                                System.Console.WriteLine($"      {prop.Name}: {prop.Value}");
                        }
                    }

                    System.Console.WriteLine();
                }

                if (result.TryGetProperty("cursor", out var nextCursor) && nextCursor.ValueKind != JsonValueKind.Null)
                {
                    System.Console.WriteLine($"More events available. Next page: papi corp events --cursor \"{nextCursor.GetString()}\"");
                    System.Console.WriteLine();
                }
            }, sinceOption, untilOption, typeOption, sourceOption, limitOption, cursorOption, jsonOption);

            return command;
        }
    }
}
