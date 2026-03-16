namespace psecsapi.Combat.Terrain;

public class BlackHoleTerrainGenerator : ITerrainGenerator
{
    private const double MinEventHorizonRadius = 2000.0;
    private const double MaxEventHorizonRadius = 3200.0;

    public CombatGrid Generate(TerrainInput input, int combatSalt, Random rng)
    {
        double radius = MinEventHorizonRadius
            + rng.NextDouble() * (MaxEventHorizonRadius - MinEventHorizonRadius);

        var eventHorizon = new CombatGridObstacle(
            new Vector2D(0.0, 0.0),
            radius,
            ObstacleType.EventHorizon);

        return new CombatGrid(
            obstacles: new[] { eventHorizon },
            nebulaPatches: Array.Empty<NebulaPatch>(),
            isGlobalNebulaActive: false,
            sectorType: "BlackHole");
    }
}
