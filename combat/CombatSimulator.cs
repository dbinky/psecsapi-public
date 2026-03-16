using psecsapi.Combat.Events;
using psecsapi.Combat.Scripting;
using psecsapi.Combat.Simulation;
using psecsapi.Combat.Snapshots;
using psecsapi.Combat.Terrain;
using psecsapi.Domain.Combat;
using psecsapi.Grains.Interfaces.Space.Models;

namespace psecsapi.Combat;

/// <summary>
/// Public entry point for running a complete combat simulation.
/// Orchestrates terrain generation, starting position calculation, script executor setup,
/// and the simulation tick loop. Returns a CombatSimulationResult.
///
/// This facade encapsulates the full simulation lifecycle that was previously inline
/// in CombatInstanceRoot, allowing it to be tested and invoked without grain dependencies.
/// </summary>
public class CombatSimulator
{
    /// <summary>
    /// Runs a complete combat simulation with the given parameters.
    /// </summary>
    /// <param name="attackerSnapshots">Ship snapshots for the attacking fleet (positions should be set).</param>
    /// <param name="defenderSnapshots">Ship snapshots for the defending fleet (positions should be set).</param>
    /// <param name="grid">The combat grid with terrain obstacles.</param>
    /// <param name="attackerOnTick">Script callback for attacker ships (null for no script).</param>
    /// <param name="defenderOnTick">Script callback for defender ships (null for no script).</param>
    /// <param name="randomSeed">Deterministic seed for the simulation RNG.</param>
    /// <param name="sectorType">Sector type for environmental effects and tick limits.</param>
    /// <returns>The simulation result with outcome, events, and ship data.</returns>
    public CombatSimulationResult RunSimulation(
        List<CombatShipSnapshot> attackerSnapshots,
        List<CombatShipSnapshot> defenderSnapshots,
        CombatGrid grid,
        Func<CombatShipSnapshot, CombatSimulationState, List<ScriptCommand>>? attackerOnTick,
        Func<CombatShipSnapshot, CombatSimulationState, List<ScriptCommand>>? defenderOnTick,
        int randomSeed,
        SectorType sectorType)
    {
        var simulation = new CombatSimulation(
            grid, attackerSnapshots, defenderSnapshots,
            attackerOnTick, defenderOnTick,
            new Random(randomSeed), sectorType);

        return simulation.Run();
    }

    /// <summary>
    /// Generates terrain for the given sector type and input data.
    /// </summary>
    public CombatGrid GenerateTerrain(SectorType sectorType, TerrainInput input, int combatSalt, Random rng)
    {
        var terrainGenerator = TerrainGeneratorFactory.GetGenerator(sectorType);
        return terrainGenerator.Generate(input, combatSalt, rng);
    }

    /// <summary>
    /// Calculates starting positions for both fleets on the combat grid.
    /// </summary>
    public (Vector2D[] AttackerPositions, Vector2D[] DefenderPositions) CalculateStartingPositions(
        double maxSensorRange,
        int attackerCount,
        int defenderCount,
        Random rng,
        CombatGridObstacle[] obstacles)
    {
        return StartingPositionCalculator.CalculateStartingPositions(
            maxSensorRange, attackerCount, defenderCount, rng, obstacles);
    }
}
