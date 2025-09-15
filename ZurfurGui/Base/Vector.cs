using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ZurfurGui.Base.Helpers;

namespace ZurfurGui.Base;

public struct Vector : IEquatable<Vector>
{
    public double X { get; set; }
    public double Y { get; set; }

    public Vector() { }
    public Vector(double x, double y) { X = x; Y = y; }
    public bool Equals(Vector v) => X == v.X && Y == v.Y;
    public override bool Equals(object? obj) => obj is Vector v && Equals(v);
    public static bool operator ==(Vector a, Vector b) => a.Equals(b);
    public static bool operator !=(Vector a, Vector b) => !a.Equals(b);
    public override string ToString() => FormattableString.Invariant($"{X},{Y}");
    public string ToString(string f) => FormattableString.Invariant($"{X.ToString(f)},{Y.ToString(f)}");
    public override int GetHashCode()
    {
        var h = X.GetHashCode();
        h += HashMix(h) + Y.GetHashCode();
        return HashMix(h);
    }
    public Point ToPoint => new Point(X, Y);
    public Size ToSize => new Size(X, Y);

    public static Vector operator +(Vector a, Vector b) => new Vector(a.X + b.X, a.Y + b.Y);
    public static Vector operator -(Vector a, Vector b) => new Vector(a.X - b.X, a.Y - b.Y);
    public static Vector operator -(Vector a) => new Vector(-a.X, -a.Y);
    public static Vector operator *(Vector a, double scale) => new Vector(a.X * scale, a.Y * scale);
    public static Vector operator *(double scale, Vector b) => new Vector(scale * b.X, scale * b.Y);
    public static Vector operator /(Vector a, double scale) => new Vector(a.X / scale, a.Y / scale);
    public static Vector operator /(double scale, Vector b) => new Vector(scale / b.X, scale / b.Y);

    public Vector MaxZero => new Vector(Math.Max(X, 0), Math.Max(Y, 0));

    public Vector Min(Vector v)
    {
        return new Vector(Math.Min(X, v.X), Math.Min(Y, v.Y));
    }
    public Vector Max(Vector v)
    {
        return new Vector(Math.Max(X, v.X), Math.Max(Y, v.Y));
    }

}
