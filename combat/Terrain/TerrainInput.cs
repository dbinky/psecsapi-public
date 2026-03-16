namespace psecsapi.Combat.Terrain;

/// <summary>
/// Lightweight input for terrain generation, decoupled from grain-level SectorDetail.
/// Built by the caller from SectorDetail data.
/// </summary>
public record TerrainInput(
    StarSystemInput? StarSystem,
    BlackHoleInput? BlackHole,
    NebulaInput? Nebula,
    RubbleInput? Rubble
);

public record StarSystemInput(
    List<StarInput> Stars,
    List<OrbitalInput> Orbitals
);

public record StarInput(decimal Mass);

public record OrbitalInput(
    int OrbitalPosition,
    OrbitalInputType Type,
    bool HasPlanet,
    bool HasAsteroidBelt
);

public enum OrbitalInputType
{
    Planet,
    AsteroidBelt,
    Station,
    Other
}

public record BlackHoleInput(double EventHorizonRadius);

public record NebulaInput(double Density, int PatchCount);

public record RubbleInput(int Density);
