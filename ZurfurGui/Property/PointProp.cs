using ZurfurGui.Base;

namespace ZurfurGui.Property;

/// <summary>
/// A type-safe Point that allows each component to be optional.  Defaults to null.
/// </summary>
public readonly record struct PointProp(DoubleProp X, DoubleProp Y)
    : IProperty<PointProp>
{
    public PointProp() : this(new(), new()) { }
    public override string ToString() => $"X: {X}, Y: {Y}";
    public string ToString(string format) => $"X: {X.ToString(format)}, Y: {Y.ToString(format)}";

    /// <summary>
    /// Returns true if all components are non-null.
    /// </summary>
    public bool IsComplete => X.HasValue && Y.HasValue;

    /// <summary>
    /// Return this value, or the other only if this is null (for each component).
    /// </summary>
    public PointProp Or(PointProp other) 
        => new PointProp(X.Or(other.X), Y.Or(other.Y));

    /// <summary>
    /// Return this value, or the other only if this is null (for each component).
    /// </summary>
    public Point Or(double other) 
        => new Point(X.Or(other), Y.Or(other));

    /// <summary>
    /// Return this value, or the other only if this is null (for each component).
    /// </summary>
    public Point Or(Point other)
        => new Point(X.Or(other.X), Y.Or(other.Y));

    /// <summary>
    /// Interpolate from this value to the destination.  
    /// </summary>
    public PointProp Interpolate(PointProp destination, double percent)
    {
        return new PointProp(
            X.Interpolate(destination.X, percent),
            Y.Interpolate(destination.Y, percent)
        );
    }

    /// <summary>
    /// Implicit conversion from Point to PointQ.
    /// </summary>
    public static implicit operator PointProp(Point value)
        => new PointProp(value.X, value.Y);

    /// <summary>
    /// Implicit conversion from Point? to PointQ.
    /// </summary>
    public static implicit operator PointProp(Point? value) 
        => value.HasValue ? value : new PointProp();


}
