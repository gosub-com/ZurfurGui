using ZurfurGui.Base.Helpers;

namespace ZurfurGui.Base;

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
        var h = new Hasher(Width.GetHashCode());
        h.Add(Height.GetHashCode());
        return h.GetHashCode();
    }
    public Vector ToVector => new Vector(Width, Height);
    public static Size operator +(Size a, Vector b) => new Size(a.Width+b.X, a.Height+b.Y);
    public static Size operator +(Vector a, Size b) => new Size(a.X + b.Width, a.Y + b.Height);
    public static Vector operator -(Size a, Size b) => new Vector(a.Width-b.Width, a.Height-b.Height);
    public static Size operator -(Vector a, Size b) => new Size(a.X - b.Width, a.Y - b.Height);
    public static Size operator -(Size a, Vector b) => new Size(a.Width - b.X, a.Height - b.Y);
    public static Size operator -(Size a) => new Size(-a.Width, -a.Height);
    public static Size operator *(Size a, double scale) => new Size(a.Width*scale, a.Height*scale);
    public static Size operator *(double scale, Size b) => new Size(scale*b.Width, scale*b.Height);
    public static Size operator /(Size a, double scale) => new Size(a.Width/scale, a.Height/scale);
    public static Size operator /(double scale, Size b) => new Size(scale/b.Width, scale/b.Height);

    public Size MaxZero => new Size(Math.Max(Width, 0), Math.Max(Height, 0));

    public Size Min(Size s)
    {
        return new Size(Math.Min(Width, s.Width), Math.Min(Height, s.Height));
    }
    public Size Max(Size s)
    {
        return new Size(Math.Max(Width, s.Width), Math.Max(Height, s.Height));
    }
    public static Size Min(Size a, Size b)
    {
        return new Size(Math.Min(a.Width, b.Width), Math.Min(a.Height, b.Height));
    }
    public static Size Max(Size a, Size b)
    {
        return new Size(Math.Max(a.Width, b.Width), Math.Max(a.Height, b.Height));
    }

    public Size Clamp(Size min, Size max)
    {
        return new Size(
            Math.Clamp(Width, min.Width, max.Width),
            Math.Clamp(Height, min.Height, max.Height));
    }

    public Size Inflate(double thickness)
    {
        return Inflate(new Thickness(thickness));
    }

    public Size Inflate(Thickness thickness)
    {
        return new Size(
            Math.Max(0, Width + thickness.Left + thickness.Right),
            Math.Max(0, Height + thickness.Top + thickness.Bottom));
    }

    public Size Deflate(double thickness)
    {
        return Deflate(new Thickness(thickness));
    }

    public Size Deflate(Thickness thickness)
    {
        return new Size(
            Math.Max(0, Width - thickness.Left - thickness.Right),
            Math.Max(0, Height - thickness.Top - thickness.Bottom));
    }



}
