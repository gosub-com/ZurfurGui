using ZurfurGui.Base;

namespace ZurfurGui.Property;

/// <summary>
/// Represents an alignment property with horizontal and vertical components. Both fields are optional.
/// </summary>
public readonly record struct AlignProp(AlignHorizontal? Horizontal, AlignVertical? Vertical)
    : IProperty<AlignProp>
{
    /// <summary>
    /// Default constructor initializes both fields to null.
    /// </summary>
    public AlignProp() : this(null, null) { }

    /// <summary>
    /// Indicates whether both fields are non-null.
    /// </summary>
    public bool IsComplete => Horizontal.HasValue && Vertical.HasValue;

    /// <summary>
    /// Returns this value, or the other only if this is null (for each component).
    /// </summary>
    public AlignProp Or(AlignProp other)
        => new AlignProp(Horizontal ?? other.Horizontal, Vertical ?? other.Vertical);

    /// <summary>
    /// Interpolates from this value to the destination. Returns the destination immediately.
    /// </summary>
    public AlignProp Interpolate(AlignProp destination, double percent)
    {
        // Alignment cannot be interpolated, so return the destination.
        return destination;
    }

    /// <summary>
    /// Returns a string representation of the AlignProp.
    /// </summary>
    public override string ToString()
        => $"Horizontal: {Horizontal?.ToString() ?? "null"}, Vertical: {Vertical?.ToString() ?? "null"}";

    /// <summary>
    /// Implicit conversion from a tuple (AlignHorizontal?, AlignVertical?) to AlignProp.
    /// </summary>
    public static implicit operator AlignProp((AlignHorizontal? Horizontal, AlignVertical? Vertical) value)
        => new AlignProp(value.Horizontal, value.Vertical);
}