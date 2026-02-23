using System.CommandLine;
using System.Text.Json;
using psecsapi.Console.Infrastructure.Configuration;
using psecsapi.Console.Infrastructure.Http;

namespace psecsapi.Console.Commands.Catalog
{
    public class CatalogCommand : ICommand
    {
        private readonly AuthenticatedHttpClient _client;
        private readonly CliConfig _config;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public CatalogCommand(AuthenticatedHttpClient client, CliConfig config)
        {
            _client = client;
            _config = config;
        }

        public Command Build()
        {
            var command = new Command("catalog", "View discovered resources and known locations");

            command.AddCommand(BuildResourcesCommand());
            command.AddCommand(BuildSectorsCommand());

            return command;
        }

        #region Resources Commands

        private Command BuildResourcesCommand()
        {
            var command = new Command("resources", "View and manage discovered resources catalog");

            // Add subcommands
            command.AddCommand(BuildResourcesListCommand());
            command.AddCommand(BuildResourcesFavoriteCommand());
            command.AddCommand(BuildResourcesUnfavoriteCommand());
            command.AddCommand(BuildResourcesNoteCommand());

            // Make list the default action when no subcommand is specified
            var typeOption = new Option<string?>("--type", "Filter by resource type (Mineral, Chemical, Flora, Fauna, Microscopic)");
            typeOption.AddAlias("-t");

            var classOption = new Option<string?>("--class", "Filter by resource class (Metal, Ore, Gemstone, Gas, etc.)");
            classOption.AddAlias("-c");

            var favoritesOption = new Option<bool>("--favorites", () => false, "Show only favorited resources");
            favoritesOption.AddAlias("-f");

            var idOption = new Option<Guid?>("--id", "Show details for a specific catalog entry");

            command.AddOption(typeOption);
            command.AddOption(classOption);
            command.AddOption(favoritesOption);
            command.AddOption(idOption);

            command.SetHandler(async (type, @class, favorites, id) =>
            {
                if (id.HasValue)
                {
                    await ShowResourceDetail(id.Value);
                }
                else
                {
                    await ListResources(type, @class, favorites);
                }
            }, typeOption, classOption, favoritesOption, idOption);

            return command;
        }

        private Command BuildResourcesListCommand()
        {
            var typeOption = new Option<string?>("--type", "Filter by resource type (Mineral, Chemical, Flora, Fauna, Microscopic)");
            typeOption.AddAlias("-t");

            var classOption = new Option<string?>("--class", "Filter by resource class (Metal, Ore, Gemstone, Gas, etc.)");
            classOption.AddAlias("-c");

            var favoritesOption = new Option<bool>("--favorites", () => false, "Show only favorited resources");
            favoritesOption.AddAlias("-f");

            var idOption = new Option<Guid?>("--id", "Show details for a specific catalog entry");

            var command = new Command("list", "List discovered resources")
            {
                typeOption, classOption, favoritesOption, idOption
            };

            command.SetHandler(async (type, @class, favorites, id) =>
            {
                if (id.HasValue)
                {
                    await ShowResourceDetail(id.Value);
                }
                else
                {
                    await ListResources(type, @class, favorites);
                }
            }, typeOption, classOption, favoritesOption, idOption);

            return command;
        }

        private async Task ListResources(string? type, string? @class, bool favorites)
        {
            var corpId = _config.User.DefaultCorpId;
            if (!corpId.HasValue)
            {
                System.Console.WriteLine("Error: No default corporation set. Use 'config set-corp <corp-id>' first.");
                return;
            }

            var queryParams = new List<string>();
            if (!string.IsNullOrEmpty(type)) queryParams.Add($"type={type}");
            if (!string.IsNullOrEmpty(@class)) queryParams.Add($"resourceClass={@class}");
            if (favorites) queryParams.Add("favoritesOnly=true");

            var query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            var response = await _client.GetAsync($"/api/corp/{corpId.Value}/catalog{query}");
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                System.Console.WriteLine(content);
                return;
            }

            var entries = JsonSerializer.Deserialize<List<CatalogResourceEntryResponse>>(content, JsonOptions);

            if (entries == null || entries.Count == 0)
            {
                System.Console.WriteLine("No resources discovered yet.");
                System.Console.WriteLine("Use 'fleet scan deep <fleet-id> <sector-id>' to discover resources.");
                return;
            }

            // Format as table
            System.Console.WriteLine();
            System.Console.WriteLine($"{"Name",-28} {"Type",-12} {"Class",-10} {"Density",-8} {"Sector",-20} {"Fav",3}");
            System.Console.WriteLine(new string('-', 83));

            foreach (var entry in entries)
            {
                var fav = entry.IsFavorite ? "*" : "";
                var name = entry.Name.Length > 27 ? entry.Name[..24] + "..." : entry.Name;
                var sector = entry.SectorName.Length > 19 ? entry.SectorName[..16] + "..." : entry.SectorName;
                System.Console.WriteLine($"{name,-28} {entry.Type,-12} {entry.Class,-10} {entry.Density:F2,-8} {sector,-20} {fav,3}");
            }

            System.Console.WriteLine();
            System.Console.WriteLine($"Total: {entries.Count} resource(s)");
        }

        private async Task ShowResourceDetail(Guid entryId)
        {
            var corpId = _config.User.DefaultCorpId;
            if (!corpId.HasValue)
            {
                System.Console.WriteLine("Error: No default corporation set. Use 'config set-corp <corp-id>' first.");
                return;
            }

            var response = await _client.GetAsync($"/api/corp/{corpId.Value}/catalog/{entryId}");
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                System.Console.WriteLine(content);
                return;
            }

            var entry = JsonSerializer.Deserialize<CatalogResourceEntryResponse>(content, JsonOptions);

            if (entry == null)
            {
                System.Console.WriteLine("Error: Unable to parse response.");
                return;
            }

            System.Console.WriteLine();
            System.Console.WriteLine($"Resource: {entry.Name}");
            System.Console.WriteLine($"Entry ID: {entry.EntryId}");
            System.Console.WriteLine($"Raw Resource ID: {entry.RawResourceId}");
            System.Console.WriteLine();
            System.Console.WriteLine("Taxonomy:");
            System.Console.WriteLine($"  Group: {entry.Group}");
            System.Console.WriteLine($"  Type:  {entry.Type}");
            System.Console.WriteLine($"  Class: {entry.Class}");
            System.Console.WriteLine($"  Order: {entry.Order}");
            System.Console.WriteLine();
            System.Console.WriteLine("Stats:");
            System.Console.WriteLine($"  Density: {entry.Density:F4}");
            foreach (var prop in entry.Properties)
            {
                System.Console.WriteLine($"  {prop.Key}: {prop.Value?.ToString() ?? "N/A"}");
            }
            System.Console.WriteLine();
            System.Console.WriteLine("Location:");
            System.Console.WriteLine($"  Sector: {entry.SectorName}");
            System.Console.WriteLine($"  Sector ID: {entry.SectorId}");
            if (entry.OrbitalPosition.HasValue)
            {
                System.Console.WriteLine($"  Orbital Position: {entry.OrbitalPosition}");
            }
            else
            {
                System.Console.WriteLine("  Orbital Position: Sector-level");
            }
            System.Console.WriteLine();
            System.Console.WriteLine("Discovery:");
            System.Console.WriteLine($"  Discovered: {entry.DiscoveredAt:yyyy-MM-dd HH:mm:ss} UTC");
            System.Console.WriteLine($"  By User: {entry.DiscoveredByUserId}");
            System.Console.WriteLine($"  Favorite: {(entry.IsFavorite ? "Yes" : "No")}");
            if (!string.IsNullOrEmpty(entry.Note))
            {
                System.Console.WriteLine();
                System.Console.WriteLine("Note:");
                System.Console.WriteLine($"  {entry.Note}");
            }
        }

        private Command BuildResourcesFavoriteCommand()
        {
            var entryIdArg = new Argument<Guid>("entry-id", "The catalog entry ID to favorite");

            var command = new Command("favorite", "Mark a resource as favorite") { entryIdArg };
            command.SetHandler(async (entryId) =>
            {
                var corpId = _config.User.DefaultCorpId;
                if (!corpId.HasValue)
                {
                    System.Console.WriteLine("Error: No default corporation set. Use 'config set-corp <corp-id>' first.");
                    return;
                }

                var response = await _client.PostAsync($"/api/corp/{corpId.Value}/catalog/{entryId}/favorite", new { });
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    System.Console.WriteLine($"Resource {entryId} favorited.");
                }
                else
                {
                    System.Console.WriteLine(content);
                }
            }, entryIdArg);

            return command;
        }

        private Command BuildResourcesUnfavoriteCommand()
        {
            var entryIdArg = new Argument<Guid>("entry-id", "The catalog entry ID to unfavorite");

            var command = new Command("unfavorite", "Remove favorite from a resource") { entryIdArg };
            command.SetHandler(async (entryId) =>
            {
                var corpId = _config.User.DefaultCorpId;
                if (!corpId.HasValue)
                {
                    System.Console.WriteLine("Error: No default corporation set. Use 'config set-corp <corp-id>' first.");
                    return;
                }

                var response = await _client.DeleteAsync($"/api/corp/{corpId.Value}/catalog/{entryId}/favorite");
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    System.Console.WriteLine($"Resource {entryId} unfavorited.");
                }
                else
                {
                    System.Console.WriteLine(content);
                }
            }, entryIdArg);

            return command;
        }

        private Command BuildResourcesNoteCommand()
        {
            var entryIdArg = new Argument<Guid>("entry-id", "The catalog entry ID");

            var setOption = new Option<string?>("--set", "Set the note content (max 500 characters)");
            setOption.AddAlias("-s");

            var clearOption = new Option<bool>("--clear", () => false, "Clear the note");
            clearOption.AddAlias("-c");

            var command = new Command("note", "Manage catalog entry notes") { entryIdArg, setOption, clearOption };
            command.SetHandler(async (entryId, setNote, clear) =>
            {
                var corpId = _config.User.DefaultCorpId;
                if (!corpId.HasValue)
                {
                    System.Console.WriteLine("Error: No default corporation set. Use 'config set-corp <corp-id>' first.");
                    return;
                }

                if (clear)
                {
                    var response = await _client.DeleteAsync($"/api/corp/{corpId.Value}/catalog/{entryId}/note");
                    var content = await response.Content.ReadAsStringAsync();
                    if (response.IsSuccessStatusCode)
                    {
                        System.Console.WriteLine($"Note cleared for {entryId}.");
                    }
                    else
                    {
                        System.Console.WriteLine(content);
                    }
                }
                else if (!string.IsNullOrEmpty(setNote))
                {
                    if (setNote.Length > 500)
                    {
                        System.Console.WriteLine("Error: Note must be 500 characters or less.");
                        return;
                    }

                    var response = await _client.PutAsync($"/api/corp/{corpId.Value}/catalog/{entryId}/note",
                        new { content = setNote });
                    var content = await response.Content.ReadAsStringAsync();
                    if (response.IsSuccessStatusCode)
                    {
                        System.Console.WriteLine($"Note set for {entryId}.");
                    }
                    else
                    {
                        System.Console.WriteLine(content);
                    }
                }
                else
                {
                    System.Console.WriteLine("Error: Must specify --set <note> or --clear");
                }
            }, entryIdArg, setOption, clearOption);

            return command;
        }

        #endregion

        #region Sectors Commands

        private Command BuildSectorsCommand()
        {
            var command = new Command("sectors", "View and manage known sectors");

            // Add subcommands
            command.AddCommand(BuildSectorsListCommand());
            command.AddCommand(BuildSectorsFavoriteCommand());
            command.AddCommand(BuildSectorsUnfavoriteCommand());
            command.AddCommand(BuildSectorsNoteCommand());

            // Make list the default action when no subcommand is specified
            var typeOption = new Option<string?>("--type", "Filter by sector type (StarSystem, BlackHole, Nebula, Rubble, Void, Nexus, Favorites)");
            typeOption.AddAlias("-t");

            var idOption = new Option<Guid?>("--id", "Show details for a specific sector");

            var notesOption = new Option<bool>("--notes", () => false, "Include notes in output");
            notesOption.AddAlias("-n");

            command.AddOption(typeOption);
            command.AddOption(idOption);
            command.AddOption(notesOption);

            command.SetHandler(async (type, id, notes) =>
            {
                await ListSectors(type, id, notes);
            }, typeOption, idOption, notesOption);

            return command;
        }

        private Command BuildSectorsListCommand()
        {
            var typeOption = new Option<string?>("--type", "Filter by sector type (StarSystem, BlackHole, Nebula, Rubble, Void, Nexus, Favorites)");
            typeOption.AddAlias("-t");

            var idOption = new Option<Guid?>("--id", "Show details for a specific sector");

            var notesOption = new Option<bool>("--notes", () => false, "Include notes in output");
            notesOption.AddAlias("-n");

            var command = new Command("list", "List known sectors")
            {
                typeOption, idOption, notesOption
            };

            command.SetHandler(async (type, id, notes) =>
            {
                await ListSectors(type, id, notes);
            }, typeOption, idOption, notesOption);

            return command;
        }

        private async Task ListSectors(string? type, Guid? id, bool notes)
        {
            string url;

            if (id.HasValue)
            {
                url = $"/api/UserMap/{id.Value}";
            }
            else
            {
                url = string.IsNullOrEmpty(type) ? "/api/UserMap" : $"/api/UserMap?type={type}";
            }

            var response = await _client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                System.Console.WriteLine($"Error: {response.StatusCode}");
                System.Console.WriteLine(content);
                return;
            }

            if (id.HasValue)
            {
                FormatSectorDetail(content);
            }
            else
            {
                FormatSectorList(content, notes);
            }
        }

        private static void FormatSectorDetail(string json)
        {
            var sector = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);

            var name = sector.GetProperty("name").GetString() ?? "Unknown";
            var entityId = sector.GetProperty("entityId").GetString() ?? "";
            var sectorType = sector.TryGetProperty("type", out var t) && t.ValueKind != JsonValueKind.Null
                ? t.ToString() : "";
            var isFav = sector.TryGetProperty("isFavorited", out var fav) && fav.ValueKind == JsonValueKind.True;

            System.Console.WriteLine();
            System.Console.WriteLine($"=== {name} ({sectorType}) ===");
            System.Console.WriteLine($"  ID:      {entityId}");
            System.Console.WriteLine($"  Fav:     {(isFav ? "Yes" : "No")}");

            if (sector.TryGetProperty("lastMappedTimestamp", out var mapped) && mapped.ValueKind != JsonValueKind.Null)
                System.Console.WriteLine($"  Mapped:  {mapped.GetDateTime():yyyy-MM-dd HH:mm:ss}");

            // Conduits
            if (sector.TryGetProperty("conduits", out var conduits) && conduits.ValueKind == JsonValueKind.Array && conduits.GetArrayLength() > 0)
            {
                System.Console.WriteLine();
                System.Console.WriteLine("Conduits:");
                foreach (var c in conduits.EnumerateArray())
                {
                    var cId = c.TryGetProperty("entityId", out var ce) ? ce.GetString() ?? "" : "";
                    var length = c.TryGetProperty("length", out var ln) ? ln.GetInt32().ToString() : "?";
                    var width = c.TryGetProperty("width", out var wd) ? wd.GetInt32().ToString() : "?";
                    System.Console.WriteLine($"  {cId}  length: {length}  width: {width}");
                }
            }

            // Always show note in detail view
            if (sector.TryGetProperty("note", out var note) && note.ValueKind != JsonValueKind.Null)
            {
                var noteText = note.GetString() ?? "";
                if (!string.IsNullOrEmpty(noteText))
                {
                    System.Console.WriteLine();
                    System.Console.WriteLine($"Note: {noteText}");
                }
            }

            System.Console.WriteLine();
        }

        private static void FormatSectorList(string json, bool showNotes)
        {
            var sectors = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);

            if (sectors.ValueKind != JsonValueKind.Array)
            {
                System.Console.WriteLine("No sector data.");
                return;
            }

            System.Console.WriteLine();
            if (sectors.GetArrayLength() == 0)
            {
                System.Console.WriteLine("No known sectors.");
                System.Console.WriteLine("Use 'fleet scan sector <fleet-id>' to discover sectors.");
                System.Console.WriteLine();
                return;
            }

            System.Console.WriteLine($"Known sectors: {sectors.GetArrayLength()}");
            System.Console.WriteLine();
            System.Console.WriteLine($"  {"Name",-24} {"Type",-14} {"Conduits",8} {"Fav",3}");
            System.Console.WriteLine($"  {new string('-', 52)}");

            foreach (var s in sectors.EnumerateArray())
            {
                var name = s.GetProperty("name").GetString() ?? "Unknown";
                if (name.Length > 22) name = name[..20] + "..";
                var sType = s.TryGetProperty("type", out var st) && st.ValueKind != JsonValueKind.Null
                    ? st.ToString() : "";
                if (sType.Length > 12) sType = sType[..10] + "..";
                var conduitCount = s.TryGetProperty("conduits", out var cs) && cs.ValueKind == JsonValueKind.Array
                    ? cs.GetArrayLength() : 0;
                var isFav = s.TryGetProperty("isFavorited", out var fav) && fav.ValueKind == JsonValueKind.True
                    ? "*" : "";

                System.Console.WriteLine($"  {name,-24} {sType,-14} {conduitCount,8} {isFav,3}");

                if (showNotes && s.TryGetProperty("note", out var note) && note.ValueKind != JsonValueKind.Null)
                {
                    var noteText = note.GetString() ?? "";
                    if (!string.IsNullOrEmpty(noteText))
                    {
                        if (noteText.Length > 60) noteText = noteText[..57] + "...";
                        System.Console.WriteLine($"    Note: {noteText}");
                    }
                }
            }

            System.Console.WriteLine();
        }

        private Command BuildSectorsFavoriteCommand()
        {
            var sectorIdArg = new Argument<Guid>("sector-id", "The sector ID to favorite");

            var command = new Command("favorite", "Mark a sector as favorite") { sectorIdArg };
            command.SetHandler(async (sectorId) =>
            {
                var response = await _client.PostAsync($"/api/UserMap/{sectorId}/favorite", new { });
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    System.Console.WriteLine($"Sector {sectorId} favorited.");
                }
                else
                {
                    System.Console.WriteLine(content);
                }
            }, sectorIdArg);

            return command;
        }

        private Command BuildSectorsUnfavoriteCommand()
        {
            var sectorIdArg = new Argument<Guid>("sector-id", "The sector ID to unfavorite");

            var command = new Command("unfavorite", "Remove favorite from a sector") { sectorIdArg };
            command.SetHandler(async (sectorId) =>
            {
                var response = await _client.DeleteAsync($"/api/UserMap/{sectorId}/favorite");
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    System.Console.WriteLine($"Sector {sectorId} unfavorited.");
                }
                else
                {
                    System.Console.WriteLine(content);
                }
            }, sectorIdArg);

            return command;
        }

        private Command BuildSectorsNoteCommand()
        {
            var sectorIdArg = new Argument<Guid>("sector-id", "The sector ID to add/update/clear note");

            var setOption = new Option<string?>("--set", "Set the note content (max 500 characters)");
            setOption.AddAlias("-s");

            var clearOption = new Option<bool>("--clear", () => false, "Clear the note");
            clearOption.AddAlias("-c");

            var command = new Command("note", "Manage sector notes") { sectorIdArg, setOption, clearOption };
            command.SetHandler(async (sectorId, setNote, clear) =>
            {
                if (clear)
                {
                    var response = await _client.DeleteAsync($"/api/UserMap/{sectorId}/note");
                    var content = await response.Content.ReadAsStringAsync();
                    if (response.IsSuccessStatusCode)
                    {
                        System.Console.WriteLine($"Note cleared for sector {sectorId}.");
                    }
                    else
                    {
                        System.Console.WriteLine(content);
                    }
                }
                else if (!string.IsNullOrEmpty(setNote))
                {
                    if (setNote.Length > 500)
                    {
                        System.Console.WriteLine("Error: Note must be 500 characters or less.");
                        return;
                    }

                    var response = await _client.PutAsync($"/api/UserMap/{sectorId}/note", new { content = setNote });
                    var content = await response.Content.ReadAsStringAsync();
                    if (response.IsSuccessStatusCode)
                    {
                        System.Console.WriteLine($"Note set for sector {sectorId}.");
                    }
                    else
                    {
                        System.Console.WriteLine(content);
                    }
                }
                else
                {
                    System.Console.WriteLine("Error: Must specify --set <note> or --clear");
                }
            }, sectorIdArg, setOption, clearOption);

            return command;
        }

        #endregion
    }
}
