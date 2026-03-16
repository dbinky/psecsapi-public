namespace psecsapi.Combat;

public class NebulaPatch
{
    public Vector2D Position { get; }
    public double Radius { get; }

    public NebulaPatch(Vector2D position, double radius)
    {
        Position = position;
        Radius = radius;
    }

    /// <summary>
    /// Returns true if the given point is inside this dense nebula patch.
    /// </summary>
    public bool Contains(Vector2D point)
    {
        return Position.DistanceTo(point) <= Radius;
    }
}
