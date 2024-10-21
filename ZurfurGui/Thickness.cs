using static ZurfurGui.Helpers;

namespace ZurfurGui;

public struct Thickness : IEquatable<Thickness>
{
    public double Left { get; set; }
    public double Top { get; set; }
    public double Right { get; set; }
    public double Bottom { get; set; }

    public Thickness() { }
    public Thickness(double size) { Left = size;  Top = size;  Right = size;  Bottom = size; }
    public Thickness(double left, double top, double right, double bottom) { Left = left; Top = top; Right = right; Bottom = bottom; }
    public bool Equals(Thickness v) => Left == v.Left && Top == v.Top && Right == v.Right && Bottom == v.Bottom;
    public override bool Equals(object? obj) => obj is Thickness v && Equals(v);
    public static bool operator ==(Thickness a, Thickness b) => a.Equals(b);
    public static bool operator !=(Thickness a, Thickness b) => !a.Equals(b);
    public override string ToString() => FormattableString.Invariant($"{Left},{Top},{Right},{Bottom}");
    public override int GetHashCode()
    {
        var h = Left.GetHashCode();
        h += HashMix(h) + Top.GetHashCode();
        h += HashMix(h) + Right.GetHashCode();
        h += HashMix(h) + Bottom.GetHashCode();
        return HashMix(h);
    }
}
