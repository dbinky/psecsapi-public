using psecsapi.Domain.Combat;

namespace psecsapi.Combat.Terrain;

public class RubbleTerrainGenerator : ITerrainGenerator
{
    private const int MinAsteroidCount = 50;
    private const int MaxAsteroidCount = 200;
    private const double MinAsteroidRadius = 20.0;
    private const double MaxAsteroidRadius = 100.0;
    private const double SpawnExclusionRadius = 2000.0;

    public CombatGrid Generate(TerrainInput input, int combatSalt, Random rng)
    {
        int asteroidCount = rng.Next(MinAsteroidCount, MaxAsteroidCount + 1);
        var obstacles = new List<CombatGridObstacle>(asteroidCount);

        // Spawn area centers (left attacker, right defender)
        var leftSpawn = new Vector2D(CombatConstants.GridMin + 3000.0, 0.0);
        var rightSpawn = new Vector2D(CombatConstants.GridMax - 3000.0, 0.0);

        for (int i = 0; i < asteroidCount; i++)
        {
            Vector2D position;
            int safetyCounter = 0;
            do
            {
                double x = CombatConstants.GridMin + rng.NextDouble() * CombatConstants.GridSize;
                double y = CombatConstants.GridMin + rng.NextDouble() * CombatConstants.GridSize;
                position = new Vector2D(x, y);
                safetyCounter++;
            }
            while (safetyCounter < 100 &&
                   (position.DistanceTo(leftSpawn) < SpawnExclusionRadius ||
                    position.DistanceTo(rightSpawn) < SpawnExclusionRadius));

            double radius = MinAsteroidRadius + rng.NextDouble() * (MaxAsteroidRadius - MinAsteroidRadius);

            obstacles.Add(new CombatGridObstacle(position, radius, ObstacleType.Asteroid));
        }

        return new CombatGrid(
            obstacles: obstacles.ToArray(),
            nebulaPatches: Array.Empty<NebulaPatch>(),
            isGlobalNebulaActive: false,
            sectorType: "Rubble");
    }
}
