namespace psecsapi.Combat.Terrain;

public class VoidTerrainGenerator : ITerrainGenerator
{
    public CombatGrid Generate(TerrainInput input, int combatSalt, Random rng)
    {
        return new CombatGrid(
            obstacles: Array.Empty<CombatGridObstacle>(),
            nebulaPatches: Array.Empty<NebulaPatch>(),
            isGlobalNebulaActive: false,
            sectorType: "Void");
    }
}
