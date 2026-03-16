using Orleans;

namespace psecsapi.Combat;

[GenerateSerializer]
public record CombatGridConfig(
    [property: Id(0)] double GridMinX,
    [property: Id(1)] double GridMinY,
    [property: Id(2)] double GridMaxX,
    [property: Id(3)] double GridMaxY,
    [property: Id(4)] List<CombatGridObstacle> Obstacles,
    [property: Id(5)] string SectorType,
    [property: Id(6)] List<DenseNebulaPatch> DenseNebulaPatches,
    [property: Id(7)] int RandomSeed
);
