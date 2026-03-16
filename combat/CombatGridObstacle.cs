using Orleans;

namespace psecsapi.Combat;

[GenerateSerializer]
public record CombatGridObstacle(
    [property: Id(0)] Vector2D Position,
    [property: Id(1)] double Radius,
    [property: Id(2)] ObstacleType Type
);
