using Orleans;

namespace psecsapi.Combat;

[GenerateSerializer]
public enum ObstacleType
{
    Asteroid,
    Planet,
    Star,
    EventHorizon
}
