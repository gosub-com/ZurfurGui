using ZurfurGui.Base;

namespace ZurfurGui.Property;

/// <summary>
/// A type-safe Point that allows each component to be optional.  Defaults to null.
/// </summary>
public readonly record struct ThicknessProp(DoubleProp Left, DoubleProp Top, DoubleProp Right, DoubleProp Bottom)
    : IProperty<ThicknessProp>
{
    public ThicknessProp() : this(new (), new (), new(), new()) { }
    public override string ToString() => $"Left: {Left}, Top: {Top}, Right: {Right}, Bottom: {Bottom}";
    public string ToString(string format) => $"Left: {Left.ToString(format)}, Top: {Top.ToString(format)}, "
        + $"Right: {Right.ToString(format)}, Bottom: {Bottom.ToString(format)}";

    /// <summary>
    /// Returns true if all components are non-null.
    /// </summary>
    public bool IsComplete => Left.HasValue && Top.HasValue && Right.HasValue && Bottom.HasValue;

    /// <summary>
    /// Return this value, or the other only if this is null (for each component).
    /// </summary>
    public ThicknessProp Or(ThicknessProp other) => new ThicknessProp(
        Left.Or(other.Left), Top.Or(other.Top), Right.Or(other.Right), Bottom.Or(other.Bottom));

    /// <summary>
    /// Return this value, or the other only if this is null (for each component).
    /// </summary>
    public Thickness Or(double other) 
        => new Thickness(Left.Or(other), Top.Or(other), Right.Or(other), Bottom.Or(other));

    /// <summary>
    /// Return this value, or the other only if this is null (for each component).
    /// </summary>
    public Thickness Or(Thickness other) 
        => new Thickness(Left.Or(other.Left), Top.Or(other.Top), Right.Or(other.Top), Bottom.Or(other.Bottom));

    /// <summary>
    /// Implicit conversion from Thickness to ThicknessQ.
    /// </summary>
    public static implicit operator ThicknessProp(Thickness v) 
        => new ThicknessProp(v.Left, v.Top, v.Right, v.Bottom);

    /// <summary>
    /// Implicit conversion from Thickness? to ThicknessQ.
    /// </summary>
    public static implicit operator ThicknessProp(Thickness? v) 
        => v.HasValue ? v : new ThicknessProp();

}
