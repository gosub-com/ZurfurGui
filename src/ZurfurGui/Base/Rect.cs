using ZurfurGui.Base.Helpers;

namespace ZurfurGui.Base;

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
    public string ToString(string f) => FormattableString.Invariant(
        $"{X.ToString(f)},{Y.ToString(f)},{Width.ToString(f)},{Height.ToString(f)}");
    public override int GetHashCode()
    {
        var h = new Hasher(X.GetHashCode());
        h.Add(Y.GetHashCode());
        h.Add(Width.GetHashCode());
        h.Add(Height.GetHashCode());
        return h.GetHashCode();
    }
    public Size Size => new Size(Width, Height);
    public Point Position => new Point(X, Y);
    public double Right => X + Width;
    public double Bottom => Y + Height;
    public static Rect Empty => new Rect(0, 0, 0, 0);

    public bool Contains(Point p)
    {
        if (IsEmpty)
            return false;
        return p.X >= X && p.X < X + Width && p.Y >= Y && p.Y < Y + Height;
    }

    public bool IsEmpty => Width <= 0 || Height <= 0;

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

    public Rect Inflate(double thickness)
    {
        return Inflate(new Thickness(thickness));
    }

    public Rect Inflate(Thickness thickness)
    {
        var size = Size.Inflate(thickness);
        if (size.IsEmpty())
            return Empty;
        return new Rect(new Point(X - thickness.Left, Y - thickness.Top), size);
    }

    public Rect Deflate(double thickness)
    {
        return Deflate(new Thickness(thickness));
    }

    public Rect Deflate(Thickness thickness)
    {
        var size = Size.Deflate(thickness);
        if (size.IsEmpty())
            return Empty;
        return new Rect(new Point(X + thickness.Left, Y + thickness.Top), size);
    }

    public Rect Move(Vector offset)
    {
        return new Rect(X + offset.X, Y + offset.Y, Width, Height);
    }



}
