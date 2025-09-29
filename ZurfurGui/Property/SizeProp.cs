using ZurfurGui.Base;

namespace ZurfurGui.Property;

/// <summary>
/// A type-safe Size that allows each component to be optional.  Defaults to null.
/// </summary>
public readonly record struct SizeProp(DoubleProp Width, DoubleProp Height)
    : IProperty<SizeProp>
{
    public SizeProp() : this(new(), new()) { }
    public override string ToString() => $"Width: {Width}, Height: {Height}";
    public string ToString(string format) => $"Width: {Width.ToString(format)}, Height: {Height.ToString(format)}";


    /// <summary>
    /// Returns true if all components are non-null.
    /// </summary>
    public bool IsComplete => Width.HasValue && Height.HasValue;

    /// <summary>
    /// Return this value, or the other only if this is null (for each component).
    /// </summary>
    public SizeProp Or(SizeProp other) => new SizeProp(Width.Or(other.Width), Height.Or(other.Height));

    /// <summary>
    /// Return this value, or the other only if this is null (for each component).
    /// </summary>
    public Size Or(double other) => new Size(Width.Or(other), Height.Or(other));

    /// <summary>
    /// Return this value, or the other only if this is null (for each component).
    /// </summary>
    public Size Or(Size other) => new Size(Width.Or(other.Width), Height.Or(other.Height));

    /// <summary>
    /// Interpolate from this value to the destination.
    /// </summary>
    public SizeProp Interpolate(SizeProp destination, double percent)
    {
        return new SizeProp(
            Width.Interpolate(Width, percent),
            Height.Interpolate(Height, percent)
        );
    }

    /// <summary>
    /// Implicit conversion from Size to SizeQ.
    /// </summary>
    public static implicit operator SizeProp(Size value) => new SizeProp(value.Width, value.Height);

    /// <summary>
    /// Implicit conversion from Size? to SizeQ.
    /// </summary>
    public static implicit operator SizeProp(Size? value) => value.HasValue ? value : new SizeProp();


}
