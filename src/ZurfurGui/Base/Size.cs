


namespace ZurfurGui.Base;

/// <summary>Represents a mutable size defined by its width and height.</summary>
public record struct Size(double Width, double Height)
{
    public override string ToString() => FormattableString.Invariant($"{Width},{Height}");
    public string ToString(string f) => FormattableString.Invariant($"{Width.ToString(f)},{Height.ToString(f)}");
    public Vector ToVector => new Vector(Width, Height);
    public static Size operator +(Size a, Vector b) => new Size(a.Width+b.X, a.Height+b.Y);
    public static Size operator +(Vector a, Size b) => new Size(a.X + b.Width, a.Y + b.Height);
    public static Vector operator -(Size a, Size b) => new Vector(a.Width-b.Width, a.Height-b.Height);
    public static Size operator -(Size a) => new Size(-a.Width, -a.Height);
    public static Size operator *(Size a, double scale) => new Size(a.Width*scale, a.Height*scale);
    public static Size operator *(double scale, Size b) => new Size(scale*b.Width, scale*b.Height);
    public static Size operator /(Size a, double scale) => new Size(a.Width/scale, a.Height/scale);
    public static Size operator /(double scale, Size b) => new Size(scale/b.Width, scale/b.Height);

    /// <summary>Gets a size with each dimension limited to zero or greater.</summary>
    public Size MaxZero => new Size(Math.Max(Width, 0), Math.Max(Height, 0));

    /// <summary>Determines whether either dimension is zero or negative.</summary>
    public bool IsEmpty() => Width <= 0 || Height <= 0;

    /// <summary>Gets a size with zero width and height.</summary>
    public static Size Empty => new Size(0, 0);

    /// <summary>Gets the component-wise minimum of this size and another size.</summary>
    public Size Min(Size s)
    {
        return new Size(Math.Min(Width, s.Width), Math.Min(Height, s.Height));
    }
    /// <summary>Gets the component-wise maximum of this size and another size.</summary>
    public Size Max(Size s)
    {
        return new Size(Math.Max(Width, s.Width), Math.Max(Height, s.Height));
    }
    /// <summary>Gets the component-wise minimum of two sizes.</summary>
    public static Size Min(Size a, Size b)
    {
        return new Size(Math.Min(a.Width, b.Width), Math.Min(a.Height, b.Height));
    }
    /// <summary>Gets the component-wise maximum of two sizes.</summary>
    public static Size Max(Size a, Size b)
    {
        return new Size(Math.Max(a.Width, b.Width), Math.Max(a.Height, b.Height));
    }

    /// <summary>Clamps each dimension to the corresponding minimum and maximum bounds.</summary>
    public Size Clamp(Size min, Size max)
    {
        return new Size(
            Math.Clamp(Width, min.Width, max.Width),
            Math.Clamp(Height, min.Height, max.Height));
    }

    /// <summary>Expands both dimensions by twice the specified thickness.</summary>
    /// <returns>The expanded size, with dimensions limited to zero or greater.</returns>
    public Size Inflate(double thickness)
    {
        return Inflate(new Thickness(thickness));
    }

    /// <summary>Expands the dimensions by the specified side thicknesses.</summary>
    /// <returns>The expanded size, with dimensions limited to zero or greater.</returns>
    public Size Inflate(Thickness thickness)
    {
        return new Size(
            Math.Max(0, Width + thickness.Left + thickness.Right),
            Math.Max(0, Height + thickness.Top + thickness.Bottom));
    }

    /// <summary>Contracts both dimensions by twice the specified thickness.</summary>
    /// <returns>The contracted size, with dimensions limited to zero or greater.</returns>
    public Size Deflate(double thickness)
    {
        return Deflate(new Thickness(thickness));
    }

    /// <summary>Contracts the dimensions by the specified side thicknesses.</summary>
    /// <returns>The contracted size, with dimensions limited to zero or greater.</returns>
    public Size Deflate(Thickness thickness)
    {
        return new Size(
            Math.Max(0, Width - thickness.Left - thickness.Right),
            Math.Max(0, Height - thickness.Top - thickness.Bottom));
    }



}
