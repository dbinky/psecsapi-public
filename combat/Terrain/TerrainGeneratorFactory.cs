using psecsapi.Grains.Interfaces.Space.Models;

namespace psecsapi.Combat.Terrain;

public static class TerrainGeneratorFactory
{
    /// <summary>
    /// Returns the terrain generator for the given sector type.
    /// Throws InvalidOperationException for Nexus (combat prohibited).
    /// Throws ArgumentOutOfRangeException for unknown sector types.
    /// </summary>
    public static ITerrainGenerator GetGenerator(SectorType sectorType)
    {
        return sectorType switch
        {
            SectorType.Void => new VoidTerrainGenerator(),
            SectorType.Rubble => new RubbleTerrainGenerator(),
            SectorType.Nebula => new NebulaTerrainGenerator(),
            SectorType.StarSystem => new StarSystemTerrainGenerator(),
            SectorType.BlackHole => new BlackHoleTerrainGenerator(),
            SectorType.Nexus => throw new InvalidOperationException(
                "Combat is prohibited in Nexus sectors."),
            _ => throw new ArgumentOutOfRangeException(
                nameof(sectorType), sectorType, "Unknown sector type for terrain generation.")
        };
    }
}
