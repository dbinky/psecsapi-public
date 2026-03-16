using psecsapi.Domain.Combat;

namespace psecsapi.Combat;

public class CombatGrid
{
    // === Grid Bounds (from CombatConstants) ===
    public double Width { get; } = CombatConstants.GridSize;
    public double Height { get; } = CombatConstants.GridSize;
    public double MinX { get; } = CombatConstants.GridMin;
    public double MinY { get; } = CombatConstants.GridMin;
    public double MaxX { get; } = CombatConstants.GridMax;
    public double MaxY { get; } = CombatConstants.GridMax;

    // === Terrain Data ===
    public CombatGridObstacle[] Obstacles { get; }
    public NebulaPatch[] NebulaPatches { get; }
    public bool IsGlobalNebulaActive { get; }
    public string SectorType { get; }

    public CombatGrid(
        CombatGridObstacle[] obstacles,
        NebulaPatch[] nebulaPatches,
        bool isGlobalNebulaActive,
        string sectorType)
    {
        Obstacles = obstacles;
        NebulaPatches = nebulaPatches;
        IsGlobalNebulaActive = isGlobalNebulaActive;
        SectorType = sectorType;
    }

    /// <summary>
    /// Returns true if the position is within the grid boundaries.
    /// </summary>
    public bool IsInBounds(Vector2D position)
    {
        return position.X >= MinX && position.X <= MaxX
            && position.Y >= MinY && position.Y <= MaxY;
    }

    /// <summary>
    /// Returns true if the position is inside any obstacle's radius.
    /// </summary>
    public bool IsInsideObstacle(Vector2D position)
    {
        for (int i = 0; i < Obstacles.Length; i++)
        {
            var obstacle = Obstacles[i];
            if (obstacle.Position.DistanceTo(position) <= obstacle.Radius)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Returns all obstacles whose circular area intersects the line segment from -> to.
    /// Uses point-to-line-segment distance vs obstacle radius.
    /// </summary>
    public CombatGridObstacle[] GetObstaclesInPath(Vector2D from, Vector2D to)
    {
        var result = new List<CombatGridObstacle>();
        for (int i = 0; i < Obstacles.Length; i++)
        {
            var obstacle = Obstacles[i];
            double dist = PointToSegmentDistance(obstacle.Position, from, to);
            if (dist <= obstacle.Radius)
                result.Add(obstacle);
        }
        return result.ToArray();
    }

    /// <summary>
    /// Returns obstacles that block line-of-sight between two positions.
    /// Energy weapons: ALL obstacles block LOS.
    /// Kinetic weapons: only obstacles with radius > SmallObstacleMaxRadius (100) block.
    /// </summary>
    public CombatGridObstacle[] GetObstaclesBlockingLOS(Vector2D from, Vector2D to, bool kineticMode)
    {
        var inPath = GetObstaclesInPath(from, to);
        if (!kineticMode)
            return inPath; // energy: all obstacles block

        // kinetic: only large obstacles block
        var result = new List<CombatGridObstacle>();
        for (int i = 0; i < inPath.Length; i++)
        {
            if (inPath[i].Radius > CombatConstants.SmallObstacleMaxRadius)
                result.Add(inPath[i]);
        }
        return result.ToArray();
    }

    /// <summary>
    /// Returns true if the position is inside any dense nebula patch.
    /// Only meaningful when IsGlobalNebulaActive is true.
    /// </summary>
    public bool IsInNebulaPatch(Vector2D position)
    {
        for (int i = 0; i < NebulaPatches.Length; i++)
        {
            if (NebulaPatches[i].Contains(position))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Computes the minimum distance from a point to a line segment.
    /// </summary>
    private static double PointToSegmentDistance(Vector2D point, Vector2D segA, Vector2D segB)
    {
        double dx = segB.X - segA.X;
        double dy = segB.Y - segA.Y;
        double lengthSq = dx * dx + dy * dy;

        if (lengthSq == 0.0)
            return point.DistanceTo(segA);

        double t = ((point.X - segA.X) * dx + (point.Y - segA.Y) * dy) / lengthSq;
        t = Math.Clamp(t, 0.0, 1.0);

        var closest = new Vector2D(segA.X + t * dx, segA.Y + t * dy);
        return point.DistanceTo(closest);
    }
}
