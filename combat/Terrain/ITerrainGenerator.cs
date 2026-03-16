namespace psecsapi.Combat.Terrain;

/// <summary>
/// Generates a CombatGrid from terrain input data and a seeded RNG.
/// Each sector type has its own implementation.
/// </summary>
public interface ITerrainGenerator
{
    /// <summary>
    /// Generates the combat grid terrain for a battle.
    /// </summary>
    /// <param name="input">Terrain generation input data extracted from sector details.</param>
    /// <param name="combatSalt">A per-combat salt combined with sector ID for seeding.</param>
    /// <param name="rng">A seeded Random instance for deterministic generation.</param>
    /// <returns>A fully constructed CombatGrid ready for simulation.</returns>
    CombatGrid Generate(TerrainInput input, int combatSalt, Random rng);
}
