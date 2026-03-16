using System.CommandLine;
using System.Globalization;
using System.Text.Json;
using psecsapi.Combat;
using psecsapi.Combat.Events;
using psecsapi.Combat.Scripting;
using psecsapi.Combat.Simulation;
using psecsapi.Combat.Snapshots;
using psecsapi.Combat.Terrain;
using psecsapi.Domain.Combat;
using psecsapi.Grains.Interfaces.Space.Models;

namespace psecsapi.Console.Commands.Combat.Simulation;

/// <summary>
/// Implements the 'papi combat simulate' command.
/// Runs a local combat simulation using psecsapi.Combat directly — no API or authentication required.
/// </summary>
public static class CombatSimulateCommand
{
    private static readonly JsonSerializerOptions JsonOutputOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // Terrain types that can be used for simulation
    private static readonly Dictionary<string, SectorType> TerrainTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["void"] = SectorType.Void,
        ["starsystem"] = SectorType.StarSystem,
        ["nebula"] = SectorType.Nebula,
        ["rubble"] = SectorType.Rubble,
        ["blackhole"] = SectorType.BlackHole,
    };

    /// <summary>
    /// Maximum allowed file size for user-supplied script files (1 MB).
    /// Prevents memory exhaustion via shared fleet configs that reference large files.
    /// </summary>
    private const long MaxScriptFileSize = 1_048_576;

    public static Command Build()
    {
        var fleet1Option = new Option<string?>("--fleet1", "Fleet 1 config: 'preset:<name>' or file path");
        fleet1Option.AddAlias("-a");
        var fleet2Option = new Option<string?>("--fleet2", "Fleet 2 config: 'preset:<name>' or file path");
        fleet2Option.AddAlias("-d");
        var terrainOption = new Option<string>("--terrain", () => "void", "Terrain/sector type for the combat grid");
        terrainOption.AddAlias("-t");
        var seedOption = new Option<int?>("--seed", "Random seed for deterministic simulation");
        seedOption.AddAlias("-s");
        var outputOption = new Option<string>("--output", () => "combat-replay.bin", "Output file path for replay binary");
        outputOption.AddAlias("-o");
        var visualizeOption = new Option<bool>("--visualize", "Launch visualizer after simulation");
        visualizeOption.AddAlias("-v");
        var listPresetsOption = new Option<bool>("--list-presets", "List available fleet presets and exit");
        var listTerrainsOption = new Option<bool>("--list-terrains", "List available terrain types and exit");

        var cmd = new Command("simulate", "Run a local combat simulation (no API required)")
        {
            fleet1Option, fleet2Option, terrainOption, seedOption,
            outputOption, visualizeOption, listPresetsOption, listTerrainsOption
        };

        cmd.SetHandler(async (string? fleet1, string? fleet2, string terrain, int? seed,
            string output, bool visualize, bool listPresets, bool listTerrains) =>
        {
            if (listPresets)
            {
                PrintPresets();
                return;
            }

            if (listTerrains)
            {
                PrintTerrains();
                return;
            }

            if (string.IsNullOrEmpty(fleet1))
            {
                System.Console.Error.WriteLine("Error: --fleet1 is required for simulation.");
                Environment.ExitCode = 1;
                return;
            }

            if (string.IsNullOrEmpty(fleet2))
            {
                System.Console.Error.WriteLine("Error: --fleet2 is required for simulation.");
                Environment.ExitCode = 1;
                return;
            }

            try
            {
                await RunSimulation(fleet1, fleet2, terrain, seed, output, visualize);
            }
            catch (Exception ex)
            {
                System.Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.ExitCode = 1;
            }
        }, fleet1Option, fleet2Option, terrainOption, seedOption,
           outputOption, visualizeOption, listPresetsOption, listTerrainsOption);

        return cmd;
    }

    private static void PrintPresets()
    {
        try
        {
            var presets = FleetConfigLoader.ListPresets();
            System.Console.WriteLine();
            System.Console.WriteLine("Available fleet presets:");
            System.Console.WriteLine();
            foreach (var preset in presets)
            {
                System.Console.WriteLine($"  {preset}");
            }
            System.Console.WriteLine();
            System.Console.WriteLine("Usage: papi combat simulate --fleet1 preset:<name> --fleet2 preset:<name>");
            System.Console.WriteLine();
        }
        catch (FileNotFoundException)
        {
            System.Console.WriteLine("No presets file found. Ensure SourceData/CombatPresets/presets.json is deployed.");
        }
    }

    private static void PrintTerrains()
    {
        System.Console.WriteLine();
        System.Console.WriteLine("Available terrain types:");
        System.Console.WriteLine();
        foreach (var terrain in TerrainTypes.Keys.OrderBy(k => k))
        {
            System.Console.WriteLine($"  {terrain}");
        }
        System.Console.WriteLine();
        System.Console.WriteLine("Usage: papi combat simulate --terrain <type> ...");
        System.Console.WriteLine();
    }

    private static async Task RunSimulation(string fleet1Input, string fleet2Input,
        string terrain, int? seed, string outputPath, bool visualize)
    {
        // Resolve terrain type
        if (!TerrainTypes.TryGetValue(terrain, out var sectorType))
        {
            throw new ArgumentException(
                $"Unknown terrain '{terrain}'. Available: {string.Join(", ", TerrainTypes.Keys.OrderBy(k => k))}");
        }

        // Load fleet configs
        var fleet1Config = FleetConfigLoader.Load(fleet1Input);
        var fleet2Config = FleetConfigLoader.Load(fleet2Input);

        if (fleet1Config.Ships.Count == 0)
            throw new ArgumentException("Fleet 1 has no ships.");
        if (fleet2Config.Ships.Count == 0)
            throw new ArgumentException("Fleet 2 has no ships.");

        // Convert to snapshots
        var corpId1 = Guid.NewGuid();
        var corpId2 = Guid.NewGuid();
        var fleetId1 = Guid.NewGuid();
        var fleetId2 = Guid.NewGuid();

        var attackerSnapshots = ShipSnapshotFactory.CreateSnapshots(fleet1Config, corpId1, fleetId1);
        var defenderSnapshots = ShipSnapshotFactory.CreateSnapshots(fleet2Config, corpId2, fleetId2);

        // Resolve scripts
        var attackerOnTick = ResolveScript(fleet1Config);
        var defenderOnTick = ResolveScript(fleet2Config);

        // Build combat grid with terrain
        int randomSeed = seed ?? new Random().Next();
        var rng = new Random(randomSeed);

        var simulator = new CombatSimulator();

        // Generate terrain
        var terrainInput = BuildTerrainInput(sectorType);
        var grid = simulator.GenerateTerrain(sectorType, terrainInput, randomSeed, rng);

        // Calculate starting positions
        double maxSensorRange = Math.Max(
            attackerSnapshots.Max(s => s.SensorCapability),
            defenderSnapshots.Max(s => s.SensorCapability));

        var (attackerPositions, defenderPositions) = simulator.CalculateStartingPositions(
            maxSensorRange, attackerSnapshots.Count, defenderSnapshots.Count, rng, grid.Obstacles);

        // Apply starting positions
        for (int i = 0; i < attackerSnapshots.Count; i++)
            attackerSnapshots[i].Position = attackerPositions[i];
        for (int i = 0; i < defenderSnapshots.Count; i++)
            defenderSnapshots[i].Position = defenderPositions[i];

        // Run simulation
        var result = simulator.RunSimulation(
            attackerSnapshots, defenderSnapshots,
            grid, attackerOnTick, defenderOnTick,
            randomSeed, sectorType);

        // Serialize replay to binary
        var recorder = new CombatEventRecorder();
        RecordEvents(recorder, result, grid, attackerSnapshots, defenderSnapshots, randomSeed);
        var replayBytes = recorder.Serialize();
        var fullOutputPath = Path.GetFullPath(outputPath);
        await File.WriteAllBytesAsync(fullOutputPath, replayBytes);

        // Gather diagnostic stats from simulation events
        var movedEvents = result.Events.Where(e => e.EventType == "ShipMoved").ToList();
        var maxVelocity = 0.0;
        var totalVelocity = 0.0;
        var movedCount = 0;
        foreach (var me in movedEvents)
        {
            if (me.Detail != null)
            {
                double vx = 0, vy = 0;
                var parts = me.Detail.Split(',');
                foreach (var part in parts)
                {
                    if (part.StartsWith("VelX:") && double.TryParse(part[5..],
                            NumberStyles.Float, CultureInfo.InvariantCulture, out var vxVal))
                        vx = vxVal;
                    else if (part.StartsWith("VelY:") && double.TryParse(part[5..],
                            NumberStyles.Float, CultureInfo.InvariantCulture, out var vyVal))
                        vy = vyVal;
                }
                var speed = Math.Sqrt(vx * vx + vy * vy);
                if (speed > maxVelocity) maxVelocity = speed;
                totalVelocity += speed;
                movedCount++;
            }
        }

        var hitEvents = result.Events.Where(e =>
            e.EventType == "DamageDealt" || e.EventType == "ProjectileHit").ToList();
        int totalHits = hitEvents.Count;
        int hitsWithShield = 0;
        int hitsWithArmor = 0;
        foreach (var he in hitEvents)
        {
            if (he.Detail != null)
            {
                var parts = he.Detail.Split(' ');
                foreach (var part in parts)
                {
                    if (part.StartsWith("Shield:") && double.TryParse(part[7..],
                            NumberStyles.Float, CultureInfo.InvariantCulture, out var sv) && sv > 0)
                        hitsWithShield++;
                    else if (part.StartsWith("Armor:") && double.TryParse(part[6..],
                            NumberStyles.Float, CultureInfo.InvariantCulture, out var av) && av > 0)
                        hitsWithArmor++;
                }
            }
        }

        // Print JSON outcome to stdout
        var outcome = new
        {
            outcome = result.Outcome.ToString(),
            ticks = result.TotalTicks,
            durationSeconds = result.DurationSeconds,
            fleet1Surviving = result.SurvivingAttackerShips.Count,
            fleet2Surviving = result.SurvivingDefenderShips.Count,
            seed = randomSeed,
            replayFile = fullOutputPath,
            replayBytes = replayBytes.Length,
            diagnostics = new
            {
                maxVelocity = Math.Round(maxVelocity, 2),
                avgVelocity = movedCount > 0 ? Math.Round(totalVelocity / movedCount, 2) : 0.0,
                shipMovedEvents = movedCount,
                totalHits,
                hitsWithShieldAbsorption = hitsWithShield,
                hitsWithArmorAblation = hitsWithArmor
            }
        };

        System.Console.WriteLine(JsonSerializer.Serialize(outcome, JsonOutputOptions));

        // Launch visualizer if requested
        if (visualize)
        {
            System.Console.Error.WriteLine($"Replay saved to: {fullOutputPath}");
            System.Console.Error.WriteLine("Use 'papi combat visualize' to view the replay.");
        }
    }

    private static Func<CombatShipSnapshot, CombatSimulationState, List<ScriptCommand>>? ResolveScript(FleetConfig config)
    {
        string? scriptSource = null;

        // Prefer ScriptFile over Script name
        if (!string.IsNullOrEmpty(config.ScriptFile))
        {
            // Validate file extension and size to prevent arbitrary file read
            // and memory exhaustion via shared fleet configs
            if (!config.ScriptFile.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"Script file must have .js extension: {config.ScriptFile}");

            if (!File.Exists(config.ScriptFile))
                throw new FileNotFoundException($"Script file not found: {config.ScriptFile}");

            var fileInfo = new FileInfo(config.ScriptFile);
            if (fileInfo.Length > MaxScriptFileSize)
                throw new ArgumentException(
                    $"Script file exceeds maximum size of {MaxScriptFileSize / 1024}KB: {config.ScriptFile}");

            scriptSource = File.ReadAllText(config.ScriptFile);
        }
        else if (!string.IsNullOrEmpty(config.Script))
        {
            scriptSource = ResolveBuiltInScript(config.Script);
        }

        if (scriptSource == null)
            return null;

        // Create per-ship executors lazily keyed by ship ID
        var executors = new Dictionary<Guid, CombatScriptExecutor>();
        var source = scriptSource;

        return (ship, state) =>
        {
            if (!executors.TryGetValue(ship.ShipId, out var executor))
            {
                executor = new CombatScriptExecutor(source, CombatConstants.MaxJintStepsPerTick);
                executor.Initialize();
                executors[ship.ShipId] = executor;

                // Execute onStart on first call
                var startState = ScriptStateBuilder.BuildState(ship, state);
                executor.ExecuteOnStart(startState);
            }

            var tickState = ScriptStateBuilder.BuildState(ship, state);
            return executor.ExecuteOnTick(tickState);
        };
    }

    private static string ResolveBuiltInScript(string scriptName)
    {
        // Check for flee (remains as a C# constant since it's the engine default)
        if (scriptName.Equals("flee", StringComparison.OrdinalIgnoreCase))
            return DefaultFleeScript.Source;

        // Try loading from SourceData/CombatScripts/{name}.js
        var fileSource = FleetConfigLoader.LoadBuiltInScript(scriptName);
        if (fileSource != null)
            return fileSource;

        var available = new List<string> { "flee" };
        available.AddRange(FleetConfigLoader.ListBuiltInScripts());
        throw new ArgumentException(
            $"Unknown built-in script '{scriptName}'. Available: {string.Join(", ", available.Distinct().OrderBy(n => n))}");
    }

    private static TerrainInput BuildTerrainInput(SectorType sectorType)
    {
        return sectorType switch
        {
            SectorType.StarSystem => new TerrainInput(
                new StarSystemInput(
                    new List<StarInput> { new(1.0m) },
                    new List<OrbitalInput>()),
                null, null, null),
            SectorType.BlackHole => new TerrainInput(
                null,
                new BlackHoleInput(500.0),
                null, null),
            SectorType.Nebula => new TerrainInput(
                null, null,
                new NebulaInput(0.5, 3),
                null),
            SectorType.Rubble => new TerrainInput(
                null, null, null,
                new RubbleInput(5)),
            SectorType.Void => new TerrainInput(null, null, null, null),
            _ => new TerrainInput(null, null, null, null)
        };
    }

    private static void RecordEvents(CombatEventRecorder recorder, CombatSimulationResult result,
        CombatGrid grid, List<CombatShipSnapshot> attackers, List<CombatShipSnapshot> defenders, int seed)
    {
        // Record CombatStarted event with terrain and ship loadouts
        var terrainRecords = grid.Obstacles.Select(o => new TerrainObstacleRecord
        {
            X = o.Position.X,
            Y = o.Position.Y,
            Radius = o.Radius,
            ObstacleType = o.Type.ToString()
        }).ToList();

        var shipLoadouts = ShipLoadoutRecordBuilder.Build(attackers, defenders);

        recorder.RecordCombatStarted(
            CombatConstants.GridSize, CombatConstants.GridSize, seed,
            terrainRecords, shipLoadouts);

        // Record simulation events — same pattern as CombatInstanceRoot
        foreach (var evt in result.Events)
        {
            switch (evt.EventType)
            {
                case "ShipMoved":
                    if (evt.ShipId.HasValue && evt.Position != null)
                    {
                        // Parse velocity from detail string "VelX:1.23,VelY:4.56"
                        double velX = 0, velY = 0;
                        if (evt.Detail != null)
                        {
                            var parts = evt.Detail.Split(',');
                            foreach (var part in parts)
                            {
                                if (part.StartsWith("VelX:") && double.TryParse(part[5..],
                                        NumberStyles.Float, CultureInfo.InvariantCulture, out var vx))
                                    velX = vx;
                                else if (part.StartsWith("VelY:") && double.TryParse(part[5..],
                                        NumberStyles.Float, CultureInfo.InvariantCulture, out var vy))
                                    velY = vy;
                            }
                        }
                        recorder.RecordShipMoved(
                            evt.ShipId.Value,
                            evt.Position.X, evt.Position.Y,
                            velX, velY, evt.Tick);
                    }
                    break;

                case "WeaponFired":
                    if (evt.ShipId.HasValue && evt.TargetShipId.HasValue)
                    {
                        recorder.RecordWeaponFired(
                            evt.ShipId.Value, Guid.Empty,
                            evt.Position?.X ?? 0.0, evt.Position?.Y ?? 0.0,
                            evt.TargetShipId.Value, 0.0, 0.0, evt.Tick);
                    }
                    break;

                case "DamageDealt":
                case "ProjectileHit":
                    if (evt.TargetShipId.HasValue)
                    {
                        double structureDamage = evt.DamageDealt ?? 0.0;
                        double shieldAbsorbed = 0.0;
                        double armorAblated = 0.0;
                        Guid moduleHitId = Guid.Empty;
                        double moduleConditionDamage = 0.0;

                        string moduleHitName = "";
                        double moduleConditionAfter = 0.0;

                        // Parse shield/armor/module breakdown from Detail string
                        // Format: "Type:<type> Shield:<value> Armor:<value> Structure:<value> [ModuleHit:<name> ModuleCondition:<value>]"
                        if (evt.Detail != null)
                        {
                            var parts = evt.Detail.Split(' ');
                            foreach (var part in parts)
                            {
                                if (part.StartsWith("Shield:") && double.TryParse(part[7..],
                                        NumberStyles.Float, CultureInfo.InvariantCulture, out var sv))
                                    shieldAbsorbed = sv;
                                else if (part.StartsWith("Armor:") && double.TryParse(part[6..],
                                        NumberStyles.Float, CultureInfo.InvariantCulture, out var av))
                                    armorAblated = av;
                                else if (part.StartsWith("ModuleHit:"))
                                    moduleHitName = part[10..];
                                else if (part.StartsWith("ModuleCondition:") && double.TryParse(part[16..],
                                        NumberStyles.Float, CultureInfo.InvariantCulture, out var mc))
                                    moduleConditionAfter = mc;
                            }

                            if (!string.IsNullOrEmpty(moduleHitName))
                            {
                                // moduleConditionDamage = how much condition was lost
                                // We use conditionAfter to derive it (100 - after gives cumulative,
                                // but per-hit damage is what matters for the damage number display)
                                moduleConditionDamage = 100.0 - moduleConditionAfter;
                            }
                        }

                        double totalDamage = shieldAbsorbed + armorAblated + structureDamage;
                        recorder.RecordProjectileHit(
                            Guid.Empty, evt.TargetShipId.Value,
                            totalDamage, shieldAbsorbed, armorAblated, structureDamage,
                            moduleHitId, moduleConditionDamage, evt.Tick,
                            moduleHitName, moduleConditionAfter);
                    }
                    break;

                case "ModuleConditionChanged":
                    // Module condition changes are tracked via the ModuleHit/ModuleCondition
                    // fields in ProjectileHit events. No separate binary recording needed.
                    break;

                case "ModuleDestroyed":
                    if (evt.ShipId.HasValue)
                        recorder.RecordModuleDestroyed(evt.ShipId.Value, Guid.Empty, evt.Tick,
                            evt.Detail ?? "");
                    break;

                case "ShipDestroyed":
                    if (evt.ShipId.HasValue)
                    {
                        recorder.RecordShipDestroyed(
                            evt.ShipId.Value, Guid.Empty,
                            evt.Position?.X ?? 0.0, evt.Position?.Y ?? 0.0,
                            new List<Guid>(), evt.Tick);
                    }
                    break;

                case "ShipFled":
                    if (evt.ShipId.HasValue)
                    {
                        recorder.RecordShipFled(
                            evt.ShipId.Value,
                            evt.Position?.X ?? 0.0, evt.Position?.Y ?? 0.0,
                            evt.Tick);
                    }
                    break;

                case "EnvironmentalDamage":
                    if (evt.ShipId.HasValue)
                    {
                        recorder.RecordEnvironmentalDamage(
                            evt.ShipId.Value,
                            evt.Detail ?? "Unknown",
                            evt.DamageDealt ?? 0.0,
                            evt.Tick);
                    }
                    break;
            }
        }

        // Record CombatEnded
        var survivingShipIds = result.SurvivingAttackerShips
            .Concat(result.SurvivingDefenderShips)
            .Select(s => s.ShipId).ToList();

        recorder.RecordCombatEnded(
            result.Outcome, survivingShipIds,
            result.TotalTicks, result.DurationSeconds);
    }
}
