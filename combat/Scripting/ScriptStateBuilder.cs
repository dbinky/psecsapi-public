using psecsapi.Domain.Combat;
using psecsapi.Combat;
using psecsapi.Combat.Snapshots;

namespace psecsapi.Combat.Scripting;

/// <summary>
/// Builds the JavaScript 'state' object from combat simulation data.
/// The returned dictionary tree is directly consumable by Jint as a JS object.
///
/// This is the single canonical implementation of script state building.
/// Both CombatInstanceRoot (production) and CombatSimulateCommand (CLI) must
/// use this class — do not duplicate the state shape elsewhere.
///
/// JS property names:
///   state.myShip        — the ship whose script is executing
///   state.enemies       — alive enemy ships visible to the script
///   state.friendlies    — alive friendly ships (including myShip)
///   state.terrain       — grid obstacles
///   state.grid          — grid dimensions
///   state.tick          — current simulation tick
/// </summary>
public static class ScriptStateBuilder
{
    /// <summary>
    /// Builds the complete state object for a ship's script execution.
    /// </summary>
    /// <param name="myShip">The ship whose script is executing.</param>
    /// <param name="myFleet">All friendly ships (including myShip).</param>
    /// <param name="enemyFleet">All enemy ships visible to the script.</param>
    /// <param name="terrain">Terrain obstacles on the grid.</param>
    /// <param name="grid">Grid dimensions.</param>
    /// <param name="currentTick">Current simulation tick number.</param>
    /// <returns>Dictionary tree representing the JS state object.</returns>
    public static Dictionary<string, object> BuildState(
        CombatShipSnapshot myShip,
        List<CombatShipSnapshot> myFleet,
        List<CombatShipSnapshot> enemyFleet,
        List<CombatGridObstacle> terrain,
        CombatGrid grid,
        int currentTick)
    {
        var state = new Dictionary<string, object>
        {
            ["myShip"] = BuildMyShip(myShip),
            ["enemies"] = enemyFleet
                .Where(e => e.IsAlive && !e.HasFled)
                .Select(BuildEnemyShipSummary).ToArray(),
            ["friendlies"] = myFleet
                .Where(f => f.IsAlive && !f.HasFled)
                .Select(BuildFleetShipSummary).ToArray(),
            ["terrain"] = terrain.Select(BuildTerrainEntry).ToArray(),
            ["grid"] = BuildGrid(grid),
            ["tick"] = currentTick
        };
        return state;
    }

    /// <summary>
    /// Builds the complete state object from a CombatSimulationState.
    /// Convenience overload for callers that already have a CombatSimulationState.
    /// </summary>
    public static Dictionary<string, object> BuildState(
        CombatShipSnapshot myShip,
        Combat.Simulation.CombatSimulationState simulationState)
    {
        return BuildState(
            myShip,
            simulationState.MyFleet,
            simulationState.EnemyFleet,
            simulationState.Grid.Obstacles.ToList(),
            simulationState.Grid,
            simulationState.Tick);
    }

    private static Dictionary<string, object> BuildMyShip(CombatShipSnapshot ship)
    {
        return new Dictionary<string, object>
        {
            ["id"] = ship.ShipId.ToString(),
            ["position"] = BuildVector(ship.Position),
            ["velocity"] = BuildVector(ship.Velocity),
            ["facing"] = ship.Facing,
            ["speed"] = ship.CurrentSpeed,
            ["maxSpeed"] = ship.MaxSpeed,
            ["maxAcceleration"] = ship.MaxAcceleration,
            ["structure"] = (double)ship.CurrentStructurePoints,
            ["maxStructure"] = (double)ship.MaxStructurePoints,
            ["shieldEffectiveness"] = ship.ShieldEffectiveness,
            ["armorEffectiveness"] = ship.ArmorEffectiveness,
            ["weapons"] = ship.Weapons.Select(BuildWeapon).ToArray(),
            ["modules"] = ship.Modules.Select(BuildModule).ToArray(),
            ["computeCapacity"] = ship.ComputeCapacity,
            ["mass"] = ship.Mass,
            ["cargo"] = ship.Cargo.Select(BuildCargoEntry).ToArray(),
            ["isAlive"] = ship.IsAlive
        };
    }

    private static Dictionary<string, object> BuildFleetShipSummary(CombatShipSnapshot ship)
    {
        return new Dictionary<string, object>
        {
            ["id"] = ship.ShipId.ToString(),
            ["position"] = BuildVector(ship.Position),
            ["velocity"] = BuildVector(ship.Velocity),
            ["facing"] = ship.Facing,
            ["structure"] = (double)ship.CurrentStructurePoints,
            ["maxStructure"] = (double)ship.MaxStructurePoints,
            ["mass"] = ship.Mass,
            ["isAlive"] = ship.IsAlive
        };
    }

    private static Dictionary<string, object> BuildEnemyShipSummary(CombatShipSnapshot ship)
    {
        return new Dictionary<string, object>
        {
            ["id"] = ship.ShipId.ToString(),
            ["position"] = BuildVector(ship.Position),
            ["velocity"] = BuildVector(ship.Velocity),
            ["facing"] = ship.Facing,
            ["structure"] = (double)ship.CurrentStructurePoints,
            ["maxStructure"] = (double)ship.MaxStructurePoints,
            ["mass"] = ship.Mass,
            ["isAlive"] = ship.IsAlive
        };
    }

    private static Dictionary<string, object> BuildVector(Vector2D vec)
    {
        return new Dictionary<string, object>
        {
            ["x"] = vec.X,
            ["y"] = vec.Y
        };
    }

    private static Dictionary<string, object> BuildWeapon(WeaponSnapshot weapon)
    {
        return new Dictionary<string, object>
        {
            ["id"] = weapon.ModuleId.ToString(),
            ["damageType"] = weapon.DamageType.ToString(),
            ["baseDamage"] = weapon.BaseDamage,
            ["range"] = weapon.Range,
            ["cooldownTicks"] = weapon.CooldownTicks,
            ["coneAngle"] = weapon.ConeAngle,
            ["condition"] = (double)weapon.Condition
        };
    }

    private static Dictionary<string, object> BuildModule(ModuleSnapshot module)
    {
        return new Dictionary<string, object>
        {
            ["id"] = module.ModuleId.ToString(),
            ["name"] = module.Name,
            ["condition"] = (double)module.Condition,
            ["capabilities"] = module.Capabilities
                .Select(c => c.CapabilityType.ToString())
                .ToArray()
        };
    }

    private static Dictionary<string, object> BuildCargoEntry(CargoEntry cargo)
    {
        return new Dictionary<string, object>
        {
            ["assetId"] = cargo.AssetId.ToString(),
            ["type"] = cargo.Type.ToString()
        };
    }

    private static Dictionary<string, object> BuildTerrainEntry(CombatGridObstacle obstacle)
    {
        return new Dictionary<string, object>
        {
            ["x"] = obstacle.Position.X,
            ["y"] = obstacle.Position.Y,
            ["radius"] = obstacle.Radius,
            ["type"] = obstacle.Type.ToString()
        };
    }

    private static Dictionary<string, object> BuildGrid(CombatGrid grid)
    {
        return new Dictionary<string, object>
        {
            ["width"] = grid.Width,
            ["height"] = grid.Height,
            ["minX"] = grid.MinX,
            ["minY"] = grid.MinY,
            ["maxX"] = grid.MaxX,
            ["maxY"] = grid.MaxY
        };
    }
}
