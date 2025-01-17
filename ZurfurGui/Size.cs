﻿using static ZurfurGui.Helpers;

namespace ZurfurGui;

public struct Size : IEquatable<Size>
{
    public double Width { get; set; }
    public double Height { get; set; }

    public Size() { }
    public Size(double width, double height) { Width = width; Height = height; }
    public void Deconstruct(out double width, out double height) { width = Width; height = Height; }
    public bool Equals(Size v) => Width == v.Width && Height == v.Height;
    public override bool Equals(object? obj) => obj is Size v && Equals(v);
    public static bool operator ==(Size a, Size b) => a.Equals(b);
    public static bool operator !=(Size a, Size b) => !a.Equals(b);
    public override string ToString() => FormattableString.Invariant($"{Width},{Height}");
    public string ToString(string f) => FormattableString.Invariant($"{Width.ToString(f)},{Height.ToString(f)}");
    public override int GetHashCode()
    {
        var h = Width.GetHashCode();
        h += HashMix(h) + Height.GetHashCode();
        return HashMix(h);
    }
    public static Size operator +(Size a, Size b)
        => new Size(a.Width+b.Width, a.Height+b.Height);
    public static Size operator -(Size a, Size b)
        => new Size(a.Width-b.Width, a.Height-b.Height);
    public static Size operator -(Size a)
        => new Size(-a.Width, -a.Height);
    public static Size operator *(Size a, double scale)
        => new Size(a.Width*scale, a.Height*scale);
    public static Size operator *(double scale, Size b)
        => new Size(scale*b.Width, scale*b.Height);
    public static Size operator /(Size a, double scale)
        => new Size(a.Width/scale, a.Height/scale);
    public static Size operator /(double scale, Size b)
        => new Size(scale/b.Width, scale/b.Height);

    public Size MaxZero => new Size(Math.Max(Width, 0), Math.Max(Height, 0));

    public Size Min(Size constraint)
    {
        return new Size(Math.Min(Width, constraint.Width), Math.Min(Height, constraint.Height));
    }
    public Size Max(Size constraint)
    {
        return new Size(Math.Max(Width, constraint.Width), Math.Max(Height, constraint.Height));
    }

    public Size Inflate(Thickness thickness)
    {
        return new Size(
            Math.Max(0, Width + thickness.Left + thickness.Right),
            Math.Max(0, Height + thickness.Top + thickness.Bottom));
    }

    public Size Deflate(Thickness thickness)
    {
        return new Size(
            Math.Max(0, Width - thickness.Left - thickness.Right),
            Math.Max(0, Height - thickness.Top - thickness.Bottom));
    }


}
