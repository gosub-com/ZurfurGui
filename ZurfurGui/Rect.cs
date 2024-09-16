﻿using static ZurfurGui.Helpers;

namespace ZurfurGui;

public struct Rect : IEquatable<Rect>
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }

    public Rect() { }
    public Rect(double x, double y, double width, double height) { X = x;  Y = y; Width = width; Height = height; }
    public Rect(Point location, Size size) { X = location.X;  Y = location.Y;  Width = size.Width; Height = size.Height; }
    public bool Equals(Rect v) => X == v.X && Y == v.Y && Width == v.Width && Height == v.Height;
    public override bool Equals(object? obj) => obj is Rect v && Equals(v);
    public static bool operator ==(Rect a, Rect b) => a.Equals(b);
    public static bool operator !=(Rect a, Rect b) => !a.Equals(b);
    public override string ToString() => FormattableString.Invariant($"{X},{Y},{Width},{Height}");
    public override int GetHashCode()
    {
        var h = X.GetHashCode();
        h += HashMix(h) + Y.GetHashCode();
        h += HashMix(h) + Width.GetHashCode();
        h += HashMix(h) + Height.GetHashCode();
        return HashMix(h);
    }
    public Size Size => new Size(Width, Height);
    public Point Location => new Point(X, Y);

}