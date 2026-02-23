using System.CommandLine;
using psecsapi.Console.Infrastructure.Configuration;
using psecsapi.Console.Infrastructure.Http;

namespace psecsapi.Console.Commands.Map
{
    public class MapCommand : ICommand
    {
        private readonly AuthenticatedHttpClient _client;
        private readonly CliConfig _config;

        public MapCommand(AuthenticatedHttpClient client, CliConfig config)
        {
            _client = client;
            _config = config;
        }

        public Command Build()
        {
            var command = new Command("map", "Map and space commands (use 'catalog sectors' for sector management)");

            command.AddCommand(BuildStatsCommand());
            command.AddCommand(BuildCreateCommand());
            command.AddCommand(BuildGetCommand());
            command.AddCommand(BuildFavoriteCommand());
            command.AddCommand(BuildUnfavoriteCommand());
            command.AddCommand(BuildNoteCommand());

            return command;
        }

        private static void ShowDeprecationWarning(string newCommand)
        {
            System.Console.ForegroundColor = ConsoleColor.Yellow;
            System.Console.WriteLine($"Warning: This command is deprecated. Use '{newCommand}' instead.");
            System.Console.ResetColor();
            System.Console.WriteLine();
        }

        private Command BuildStatsCommand()
        {
            var command = new Command("stats", "Get map statistics");
            command.SetHandler(async () =>
            {
                var response = await _client.GetAsync("/api/Space/stats");
                var content = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    System.Console.WriteLine($"Error {(int)response.StatusCode}: {content}");
                    return;
                }
                System.Console.WriteLine(content);
            });
            return command;
        }

        private Command BuildCreateCommand()
        {
            var countOption = new Option<int>("--count", () => 1, "Number of sectors to create");
            countOption.AddAlias("-c");

            var command = new Command("create", "Create new sectors") { countOption };
            command.SetHandler(async (count) =>
            {
                var response = await _client.PostAsync("/api/Space", new { sectorsToCreate = count });
                var content = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    System.Console.WriteLine($"Error {(int)response.StatusCode}: {content}");
                    return;
                }
                System.Console.WriteLine(content);
            }, countOption);

            return command;
        }

        private Command BuildGetCommand()
        {
            var typeOption = new Option<string?>("--type", "Filter by sector type (StarSystem, BlackHole, Nebula, Rubble, Void, Nexus, Favorites)");
            typeOption.AddAlias("-t");

            var idOption = new Option<Guid?>("--id", "Get a specific sector by ID");

            var notesOption = new Option<bool>("--notes", () => false, "Include notes in output");
            notesOption.AddAlias("-n");

            var command = new Command("get", "[DEPRECATED] View known sectors from your personal map") { typeOption, idOption, notesOption };
            command.SetHandler(async (type, id, notes) =>
            {
                ShowDeprecationWarning("catalog sectors");

                string url;

                if (id.HasValue)
                {
                    // Get single sector (always includes note)
                    url = $"/api/UserMap/{id.Value}";
                }
                else
                {
                    // Get list of sectors
                    url = string.IsNullOrEmpty(type) ? "/api/UserMap" : $"/api/UserMap?type={type}";
                }

                var response = await _client.GetAsync(url);
                System.Console.WriteLine(await response.Content.ReadAsStringAsync());
            }, typeOption, idOption, notesOption);

            return command;
        }

        private Command BuildFavoriteCommand()
        {
            var sectorIdArg = new Argument<Guid>("sector-id", "The sector ID to favorite");

            var command = new Command("favorite", "[DEPRECATED] Mark a sector as favorite") { sectorIdArg };
            command.SetHandler(async (sectorId) =>
            {
                ShowDeprecationWarning("catalog sectors favorite");

                var response = await _client.PostAsync($"/api/UserMap/{sectorId}/favorite", new { });
                System.Console.WriteLine(await response.Content.ReadAsStringAsync());
            }, sectorIdArg);

            return command;
        }

        private Command BuildUnfavoriteCommand()
        {
            var sectorIdArg = new Argument<Guid>("sector-id", "The sector ID to unfavorite");

            var command = new Command("unfavorite", "[DEPRECATED] Remove favorite from a sector") { sectorIdArg };
            command.SetHandler(async (sectorId) =>
            {
                ShowDeprecationWarning("catalog sectors unfavorite");

                var response = await _client.DeleteAsync($"/api/UserMap/{sectorId}/favorite");
                System.Console.WriteLine(await response.Content.ReadAsStringAsync());
            }, sectorIdArg);

            return command;
        }

        private Command BuildNoteCommand()
        {
            var sectorIdArg = new Argument<Guid>("sector-id", "The sector ID to add/update/clear note");

            var setOption = new Option<string?>("--set", "Set the note content (max 500 characters)");
            setOption.AddAlias("-s");

            var clearOption = new Option<bool>("--clear", () => false, "Clear the note");
            clearOption.AddAlias("-c");

            var command = new Command("note", "[DEPRECATED] Manage sector notes") { sectorIdArg, setOption, clearOption };
            command.SetHandler(async (sectorId, setNote, clear) =>
            {
                ShowDeprecationWarning("catalog sectors note");

                if (clear)
                {
                    var response = await _client.DeleteAsync($"/api/UserMap/{sectorId}/note");
                    System.Console.WriteLine(await response.Content.ReadAsStringAsync());
                }
                else if (!string.IsNullOrEmpty(setNote))
                {
                    var response = await _client.PutAsync($"/api/UserMap/{sectorId}/note", new { content = setNote });
                    System.Console.WriteLine(await response.Content.ReadAsStringAsync());
                }
                else
                {
                    System.Console.WriteLine("{\"error\": \"Must specify --set or --clear\"}");
                }
            }, sectorIdArg, setOption, clearOption);

            return command;
        }
    }
}
