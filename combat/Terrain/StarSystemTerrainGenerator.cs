using psecsapi.Domain.Combat;

namespace psecsapi.Combat.Terrain;

public class StarSystemTerrainGenerator : ITerrainGenerator
{
    private const double MinStarRadius = 200.0;
    private const double MaxStarRadius = 500.0;
    private const double MinPlanetRadius = 50.0;
    private const double MaxPlanetRadius = 200.0;
    private const int MinBeltAsteroids = 20;
    private const int MaxBeltAsteroids = 50;
    private const double MinBeltAsteroidRadius = 10.0;
    private const double MaxBeltAsteroidRadius = 40.0;
    private const double BeltScatterRadius = 800.0;
    private const double OrbitalRadiusStep = 1500.0;
    private const double BaseOrbitalRadius = 2000.0;

    public CombatGrid Generate(TerrainInput input, int combatSalt, Random rng)
    {
        var obstacles = new List<CombatGridObstacle>();
        var starSystemInput = input.StarSystem;

        if (starSystemInput != null)
        {
            // Place stars
            PlaceStars(starSystemInput.Stars, obstacles, rng);

            // Place orbitals (planets and asteroid belts)
            PlaceOrbitals(starSystemInput.Orbitals, obstacles, rng);
        }

        return new CombatGrid(
            obstacles: obstacles.ToArray(),
            nebulaPatches: Array.Empty<NebulaPatch>(),
            isGlobalNebulaActive: false,
            sectorType: "StarSystem");
    }

    /// <summary>
    /// Places stars near the grid center. Multiple stars are offset from center.
    /// Star radius is derived from the star's Mass property, clamped to 200-500.
    /// </summary>
    private void PlaceStars(List<StarInput> stars, List<CombatGridObstacle> obstacles, Random rng)
    {
        if (stars == null || stars.Count == 0)
            return;

        for (int i = 0; i < stars.Count; i++)
        {
            var star = stars[i];

            // Offset multiple stars from center
            double offsetAngle = (2.0 * Math.PI * i) / stars.Count;
            double offsetDist = stars.Count > 1 ? 600.0 : 0.0;
            double x = Math.Cos(offsetAngle) * offsetDist;
            double y = Math.Sin(offsetAngle) * offsetDist;

            // Radius based on star mass, clamped to valid range
            double radius = MinStarRadius + ((double)star.Mass / 100.0) * (MaxStarRadius - MinStarRadius);
            radius = Math.Clamp(radius, MinStarRadius, MaxStarRadius);

            obstacles.Add(new CombatGridObstacle(new Vector2D(x, y), radius, ObstacleType.Star));
        }
    }

    /// <summary>
    /// Places orbital bodies at radial distances from center based on their OrbitalPosition index.
    /// Planets become Planet obstacles. Asteroid belts become scattered Asteroid clusters.
    /// </summary>
    private void PlaceOrbitals(List<OrbitalInput> orbitals, List<CombatGridObstacle> obstacles, Random rng)
    {
        if (orbitals == null || orbitals.Count == 0)
            return;

        for (int i = 0; i < orbitals.Count; i++)
        {
            var orbital = orbitals[i];

            // Map OrbitalPosition to a radial distance from center
            double orbitalRadius = BaseOrbitalRadius + (orbital.OrbitalPosition * OrbitalRadiusStep);

            // Place at a deterministic angle based on orbital index
            double baseAngle = (2.0 * Math.PI * orbital.OrbitalPosition) / Math.Max(orbitals.Count, 6);
            // Add small random offset to angle for visual variety
            double angleJitter = (rng.NextDouble() - 0.5) * 0.3;
            double angle = baseAngle + angleJitter;

            // Clamp orbital radius so it stays within the grid
            orbitalRadius = Math.Min(orbitalRadius, CombatConstants.GridMax - 500.0);

            double centerX = Math.Cos(angle) * orbitalRadius;
            double centerY = Math.Sin(angle) * orbitalRadius;

            if (orbital.Type == OrbitalInputType.Planet && orbital.HasPlanet)
            {
                // Planet radius from planet type characteristics, clamped
                double planetRadius = MinPlanetRadius + rng.NextDouble() * (MaxPlanetRadius - MinPlanetRadius);
                obstacles.Add(new CombatGridObstacle(
                    new Vector2D(centerX, centerY), planetRadius, ObstacleType.Planet));
            }
            else if (orbital.Type == OrbitalInputType.AsteroidBelt && orbital.HasAsteroidBelt)
            {
                // Scatter small asteroids around the belt position
                int asteroidCount = rng.Next(MinBeltAsteroids, MaxBeltAsteroids + 1);
                for (int j = 0; j < asteroidCount; j++)
                {
                    double scatterAngle = rng.NextDouble() * 2.0 * Math.PI;
                    double scatterDist = rng.NextDouble() * BeltScatterRadius;
                    double ax = centerX + Math.Cos(scatterAngle) * scatterDist;
                    double ay = centerY + Math.Sin(scatterAngle) * scatterDist;
                    double asteroidRadius = MinBeltAsteroidRadius
                        + rng.NextDouble() * (MaxBeltAsteroidRadius - MinBeltAsteroidRadius);

                    // Only add if within grid bounds
                    var pos = new Vector2D(ax, ay);
                    if (pos.X >= CombatConstants.GridMin && pos.X <= CombatConstants.GridMax &&
                        pos.Y >= CombatConstants.GridMin && pos.Y <= CombatConstants.GridMax)
                    {
                        obstacles.Add(new CombatGridObstacle(pos, asteroidRadius, ObstacleType.Asteroid));
                    }
                }
            }
        }
    }
}
