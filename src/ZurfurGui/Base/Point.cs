


namespace ZurfurGui.Base;

/// <summary>Represents a mutable point defined by its horizontal and vertical coordinates.</summary>
public record struct Point(double X, double Y)
{
    public override string ToString() => FormattableString.Invariant($"{X},{Y}");
    public string ToString(string f) => FormattableString.Invariant($"{X.ToString(f)},{Y.ToString(f)}");
    public Vector ToVector => new Vector(X, Y);
    public static Point operator +(Point a, Vector b) => new Point(a.X + b.X, a.Y + b.Y);
    public static Point operator +(Vector a, Point b) => new Point(a.X + b.X, a.Y + b.Y);
    public static Vector operator -(Point a, Point b) => new Vector(a.X - b.X, a.Y - b.Y);
    public static Point operator -(Point a, Vector b) => new Point(a.X - b.X, a.Y - b.Y);
    public static Point operator -(Point a) => new Point(-a.X, -a.Y);
    public static Point operator *(Point a, double scale) => new Point(a.X*scale, a.Y*scale);
    public static Point operator *(double scale, Point b) => new Point(scale*b.X, scale*b.Y);
    public static Point operator /(Point a, double scale) => new Point(a.X/scale, a.Y/scale);
    public static Point operator /(double scale, Point b) => new Point(scale/b.X, scale/b.Y);

    /// <summary>Gets a point with each coordinate limited to zero or greater.</summary>
    public Point MaxZero => new Point(Math.Max(X, 0), Math.Max(Y, 0));


}
