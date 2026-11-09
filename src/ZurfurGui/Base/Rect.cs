


namespace ZurfurGui.Base;

/// <summary>Represents a mutable axis-aligned rectangle defined by its position and dimensions.</summary>
public record struct Rect(double X, double Y, double Width, double Height)
{
    public Rect(Point location, Size size) : this(location.X, location.Y, size.Width, size.Height) { }
    public override string ToString() => FormattableString.Invariant($"{X},{Y},{Width},{Height}");
    public string ToString(string f) => FormattableString.Invariant(
        $"{X.ToString(f)},{Y.ToString(f)},{Width.ToString(f)},{Height.ToString(f)}");

    /// <summary>Gets the rectangle's width and height as a <see cref="Size"/>.</summary>
    public Size Size => new Size(Width, Height);

    /// <summary>Gets the rectangle's top-left position as a <see cref="Point"/>.</summary>
    public Point Position => new Point(X, Y);

    /// <summary>Gets the x-coordinate of the rectangle's right edge.</summary>
    public double Right => X + Width;

    /// <summary>Gets the y-coordinate of the rectangle's bottom edge.</summary>
    public double Bottom => Y + Height;

    /// <summary>Gets a rectangle with zero position, width, and height.</summary>
    public static Rect Empty => new Rect(0, 0, 0, 0);

    /// <summary>Determines whether the rectangle contains a point.</summary>
    /// <returns>
    /// <see langword="true"/> if the point is within the rectangle; left and top edges are
    /// inclusive, while right and bottom edges are exclusive.
    /// </returns>
    public bool Contains(Point p)
    {
        if (IsEmpty)
            return false;
        return p.X >= X && p.X < X + Width && p.Y >= Y && p.Y < Y + Height;
    }

    /// <summary>Determines whether the rectangle has a non-positive width or height.</summary>
    public bool IsEmpty => Width <= 0 || Height <= 0;

    /// <summary>Gets the smallest rectangle that contains this rectangle and another.</summary>
    public Rect Union(Rect r)
    {
        if (IsEmpty && r.IsEmpty)
            return Empty;
        if (r.IsEmpty)
            return this;
        if (IsEmpty)
            return r;
        var newX = Math.Min(r.X, X);
        var newY = Math.Min(r.Y, Y);
        var newRight = Math.Max(r.Right, Right);
        var newBottom = Math.Max(r.Bottom, Bottom);
        return new Rect(newX, newY, newRight - newX, newBottom - newY);
    }

    /// <summary>Gets the overlapping area between this rectangle and another.</summary>
    /// <returns>The intersection, or <see cref="Empty"/> if they do not overlap.</returns>
    public Rect Intersect(Rect r)
    {
        if (IsEmpty || r.IsEmpty)
            return Empty;
        var newX = Math.Max(r.X, X);
        var newY = Math.Max(r.Y, Y);
        var newRight = Math.Min(r.Right, Right);
        var newBottom = Math.Min(r.Bottom, Bottom);
        var width = newRight - newX;
        var height = newBottom - newY;
        if (width <= 0 || height <= 0)
            return Empty;
        return new Rect(newX, newY, width, height);
    }

    /// <summary>Returns a rectangle with non-negative dimensions, adjusting its position as needed.</summary>
    public Rect Normalize()
    {
        var x = X;
        var y = Y;
        var width = Width;
        var height = Height;

        if (width < 0)
        {
            x += width;
            width = Math.Abs(width);
        }

        if (height < 0)
        {
            y += height;
            height = Math.Abs(height);
        }

        return new Rect(x, y, width, height);
    }

    /// <summary>Expands the rectangle by the specified thickness on every side.</summary>
    /// <returns>The expanded rectangle, or <see cref="Empty"/> if its size is empty.</returns>
    public Rect Inflate(double thickness)
    {
        return Inflate(new Thickness(thickness));
    }

    /// <summary>Expands the rectangle by the specified thickness on each side.</summary>
    /// <returns>The expanded rectangle, or <see cref="Empty"/> if its size is empty.</returns>
    public Rect Inflate(Thickness thickness)
    {
        var size = Size.Inflate(thickness);
        if (size.IsEmpty())
            return Empty;
        return new Rect(new Point(X - thickness.Left, Y - thickness.Top), size);
    }

    /// <summary>Contracts the rectangle by the specified thickness on every side.</summary>
    /// <returns>The contracted rectangle, or <see cref="Empty"/> if its size is empty.</returns>
    public Rect Deflate(double thickness)
    {
        return Deflate(new Thickness(thickness));
    }

    /// <summary>Contracts the rectangle by the specified thickness on each side.</summary>
    /// <returns>The contracted rectangle, or <see cref="Empty"/> if its size is empty.</returns>
    public Rect Deflate(Thickness thickness)
    {
        var size = Size.Deflate(thickness);
        if (size.IsEmpty())
            return Empty;
        return new Rect(new Point(X + thickness.Left, Y + thickness.Top), size);
    }

    /// <summary>Moves the rectangle by a vector while preserving its size.</summary>
    public Rect Move(Vector offset)
    {
        return new Rect(X + offset.X, Y + offset.Y, Width, Height);
    }



}
