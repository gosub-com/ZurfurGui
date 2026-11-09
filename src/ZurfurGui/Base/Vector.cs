


namespace ZurfurGui.Base;

/// <summary>Represents a mutable two-dimensional vector.</summary>
public record struct Vector(double X, double Y)
{
    public override string ToString() => FormattableString.Invariant($"{X},{Y}");
    public string ToString(string f) => FormattableString.Invariant($"{X.ToString(f)},{Y.ToString(f)}");
    public Point ToPoint => new Point(X, Y);
    public Size ToSize => new Size(X, Y);

    public static Vector operator +(Vector a, Vector b) => new Vector(a.X + b.X, a.Y + b.Y);
    public static Vector operator -(Vector a, Vector b) => new Vector(a.X - b.X, a.Y - b.Y);
    public static Vector operator -(Vector a) => new Vector(-a.X, -a.Y);
    public static Vector operator *(Vector a, double scale) => new Vector(a.X * scale, a.Y * scale);
    public static Vector operator *(double scale, Vector b) => new Vector(scale * b.X, scale * b.Y);
    public static Vector operator /(Vector a, double scale) => new Vector(a.X / scale, a.Y / scale);
    public static Vector operator /(double scale, Vector b) => new Vector(scale / b.X, scale / b.Y);

    /// <summary>Gets a vector with each component limited to zero or greater.</summary>
    public Vector MaxZero => new Vector(Math.Max(X, 0), Math.Max(Y, 0));

    /// <summary>Gets the component-wise minimum of this vector and another vector.</summary>
    public Vector Min(Vector v)
    {
        return new Vector(Math.Min(X, v.X), Math.Min(Y, v.Y));
    }
    /// <summary>Gets the component-wise maximum of this vector and another vector.</summary>
    public Vector Max(Vector v)
    {
        return new Vector(Math.Max(X, v.X), Math.Max(Y, v.Y));
    }

}
