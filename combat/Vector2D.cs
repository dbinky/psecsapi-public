using Orleans;

namespace psecsapi.Combat;

[GenerateSerializer]
public record Vector2D([property: Id(0)] double X, [property: Id(1)] double Y)
{
    public static readonly Vector2D Zero = new(0.0, 0.0);

    public double Length() => Math.Sqrt(X * X + Y * Y);

    public Vector2D Normalize()
    {
        var len = Length();
        if (len == 0.0)
            return Zero;
        return new Vector2D(X / len, Y / len);
    }

    public double DistanceTo(Vector2D other)
    {
        var dx = other.X - X;
        var dy = other.Y - Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    public double AngleTo(Vector2D other)
    {
        var dx = other.X - X;
        var dy = other.Y - Y;
        if (dx == 0.0 && dy == 0.0)
            return 0.0;
        return Math.Atan2(dy, dx);
    }

    public Vector2D Add(Vector2D other) => new(X + other.X, Y + other.Y);
    public Vector2D Subtract(Vector2D other) => new(X - other.X, Y - other.Y);
    public Vector2D Scale(double scalar) => new(X * scalar, Y * scalar);
}
