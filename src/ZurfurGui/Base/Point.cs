using ZurfurGui.Base.Helpers;

namespace ZurfurGui.Base;

public struct Point : IEquatable<Point>
{
    public double X { get; set; }
    public double Y { get; set; }

    public Point() { }
    public Point(double x, double y) { X = x; Y = y; }
    public bool Equals(Point v) => X == v.X && Y == v.Y;
    public override bool Equals(object? obj) => obj is Point v && Equals(v);
    public static bool operator ==(Point a, Point b) => a.Equals(b);
    public static bool operator !=(Point a, Point b) => !a.Equals(b);
    public override string ToString() => FormattableString.Invariant($"{X},{Y}");
    public string ToString(string f) => FormattableString.Invariant($"{X.ToString(f)},{Y.ToString(f)}");
    public override int GetHashCode()
    {
        var h = new Hasher(X.GetHashCode());
        h.Add(Y.GetHashCode());
        return h.GetHashCode();
    }
    public Vector ToVector => new Vector(X, Y);
    public static Point operator +(Point a, Vector b) => new Point(a.X+b.X, a.Y+b.Y);
    public static Point operator +(Vector a, Point b) => new Point(a.X + b.X, a.Y + b.Y);
    public static Vector operator -(Point a, Point b) => new Vector(a.X-b.X, a.Y-b.Y);
    public static Point operator -(Vector a, Point b) => new Point(a.X - b.X, a.Y - b.Y);
    public static Point operator -(Point a, Vector b) => new Point(a.X - b.X, a.Y - b.Y);
    public static Point operator -(Point a) => new Point(-a.X, -a.Y);
    public static Point operator *(Point a, double scale) => new Point(a.X*scale, a.Y*scale);
    public static Point operator *(double scale, Point b) => new Point(scale*b.X, scale*b.Y);
    public static Point operator /(Point a, double scale) => new Point(a.X/scale, a.Y/scale);
    public static Point operator /(double scale, Point b) => new Point(scale/b.X, scale/b.Y);

    public Point MaxZero => new Point(Math.Max(X, 0), Math.Max(Y, 0));


}
