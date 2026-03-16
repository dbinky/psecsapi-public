using psecsapi.Domain.Combat;

namespace psecsapi.Combat.Terrain;

public class NebulaTerrainGenerator : ITerrainGenerator
{
    private const int MinPatchCount = 3;
    private const int MaxPatchCount = 8;
    private const double MinPatchRadius = 1000.0;
    private const double MaxPatchRadius = 3000.0;

    public CombatGrid Generate(TerrainInput input, int combatSalt, Random rng)
    {
        int patchCount = rng.Next(MinPatchCount, MaxPatchCount + 1);
        var patches = new NebulaPatch[patchCount];

        for (int i = 0; i < patchCount; i++)
        {
            double x = CombatConstants.GridMin + rng.NextDouble() * CombatConstants.GridSize;
            double y = CombatConstants.GridMin + rng.NextDouble() * CombatConstants.GridSize;
            double radius = MinPatchRadius + rng.NextDouble() * (MaxPatchRadius - MinPatchRadius);

            patches[i] = new NebulaPatch(new Vector2D(x, y), radius);
        }

        return new CombatGrid(
            obstacles: Array.Empty<CombatGridObstacle>(),
            nebulaPatches: patches,
            isGlobalNebulaActive: true,
            sectorType: "Nebula");
    }
}
