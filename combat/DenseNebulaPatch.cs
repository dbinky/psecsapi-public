using Orleans;

namespace psecsapi.Combat;

[GenerateSerializer]
public record DenseNebulaPatch(
    [property: Id(0)] Vector2D Center,
    [property: Id(1)] double Radius
);
