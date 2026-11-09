


namespace ZurfurGui.Base;

/// <summary>Represents mutable thickness values for the left, top, right, and bottom sides.</summary>
public record struct Thickness(double Left, double Top, double Right, double Bottom)
{
    public Thickness(double size) : this(size, size, size, size) { }
    public override string ToString() => FormattableString.Invariant($"{Left},{Top},{Right},{Bottom}");
    public static Thickness operator +(Thickness a, Thickness b)
        => new Thickness(a.Left + b.Left, a.Top + b.Top, a.Right + b.Right, a.Bottom + b.Bottom);

}
