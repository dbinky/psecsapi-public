using System.Reflection;
using System.Text.Json;

namespace psecsapi.Console.Commands.Combat.Simulation;

/// <summary>
/// Loads fleet configurations from JSON strings, file paths, or built-in presets.
/// Presets are loaded from SourceData/CombatPresets/presets.json relative to the executable.
/// </summary>
public static class FleetConfigLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Dispatches loading based on the input string.
    /// If the input starts with "preset:" it loads a named preset.
    /// Otherwise it treats it as a file path.
    /// </summary>
    public static FleetConfig Load(string input)
    {
        if (input.StartsWith("preset:", StringComparison.OrdinalIgnoreCase))
        {
            var presetName = input["preset:".Length..].Trim();
            return LoadPreset(presetName);
        }

        return LoadFromFile(input);
    }

    /// <summary>
    /// Deserializes a FleetConfig from a JSON string.
    /// </summary>
    public static FleetConfig LoadFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new FormatException("Fleet config JSON is empty.");

        FleetConfig? config;
        try
        {
            config = JsonSerializer.Deserialize<FleetConfig>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new FormatException($"Invalid fleet config JSON: {ex.Message}", ex);
        }

        if (config == null)
            throw new FormatException("Fleet config JSON deserialized to null.");

        return config;
    }

    /// <summary>
    /// Loads a FleetConfig from a JSON file on disk.
    /// </summary>
    public static FleetConfig LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Fleet config file not found: {filePath}", filePath);

        var json = File.ReadAllText(filePath);
        return LoadFromJson(json);
    }

    /// <summary>
    /// Loads a named preset from the built-in presets.json file.
    /// </summary>
    public static FleetConfig LoadPreset(string presetName)
    {
        var presets = LoadPresetsFile();

        if (!presets.TryGetValue(presetName, out var config))
        {
            var available = string.Join(", ", presets.Keys.OrderBy(k => k));
            throw new ArgumentException(
                $"Unknown preset '{presetName}'. Available presets: {available}",
                nameof(presetName));
        }

        return config;
    }

    /// <summary>
    /// Returns the names of all available presets.
    /// </summary>
    public static IReadOnlyList<string> ListPresets()
    {
        var presets = LoadPresetsFile();
        return presets.Keys.OrderBy(k => k).ToList();
    }

    private static Dictionary<string, FleetConfig> LoadPresetsFile()
    {
        var presetsPath = GetPresetsFilePath();

        if (!File.Exists(presetsPath))
            throw new FileNotFoundException(
                $"Presets file not found at: {presetsPath}. Ensure SourceData/CombatPresets/presets.json is deployed with the executable.",
                presetsPath);

        var json = File.ReadAllText(presetsPath);
        var presets = JsonSerializer.Deserialize<Dictionary<string, FleetConfig>>(json, JsonOptions);

        if (presets == null || presets.Count == 0)
            throw new InvalidOperationException("Presets file is empty or could not be parsed.");

        return presets;
    }

    /// <summary>
    /// Resolves a built-in script name to its JavaScript source code.
    /// Looks in SourceData/CombatScripts/{name}.js relative to the executable.
    /// Returns null if the name is not a recognized built-in script.
    /// </summary>
    public static string? LoadBuiltInScript(string scriptName)
    {
        var scriptPath = GetBuiltInScriptPath(scriptName);
        if (scriptPath != null && File.Exists(scriptPath))
            return File.ReadAllText(scriptPath);

        return null;
    }

    /// <summary>
    /// Returns the names of all available built-in combat scripts.
    /// </summary>
    public static IReadOnlyList<string> ListBuiltInScripts()
    {
        var scriptsDir = GetScriptsDirectory();
        if (!Directory.Exists(scriptsDir))
            return Array.Empty<string>();

        return Directory.GetFiles(scriptsDir, "*.js")
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .OrderBy(n => n)
            .ToList();
    }

    private static string? GetBuiltInScriptPath(string scriptName)
    {
        // Normalize: accept both "advanced-flee" and "advanced-flee.js"
        var name = scriptName.Trim();
        if (name.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
            name = name[..^3];

        // Reject path separators to prevent directory traversal.
        // Fleet configs are designed to be shared — a malicious config could
        // reference "../../etc/passwd" as a script name.
        if (name.Contains(Path.DirectorySeparatorChar) || name.Contains(Path.AltDirectorySeparatorChar))
            return null;

        var scriptsDir = GetScriptsDirectory();
        var path = Path.Combine(scriptsDir, name + ".js");

        // Verify resolved path stays within the scripts directory
        var resolvedPath = Path.GetFullPath(path);
        var resolvedScriptsDir = Path.GetFullPath(scriptsDir);
        if (!resolvedPath.StartsWith(resolvedScriptsDir, StringComparison.Ordinal))
            return null;

        return File.Exists(path) ? path : null;
    }

    private static string GetScriptsDirectory()
    {
        var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            ?? AppContext.BaseDirectory;
        return Path.Combine(assemblyDir, "SourceData", "CombatScripts");
    }

    private static string GetPresetsFilePath()
    {
        var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            ?? AppContext.BaseDirectory;
        return Path.Combine(assemblyDir, "SourceData", "CombatPresets", "presets.json");
    }
}
