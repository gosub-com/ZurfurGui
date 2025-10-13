using ZurfurGui.Base;

namespace ZurfurGui.Property;

/// <summary>
/// Represents a font property with a name and size. Both fields are optional.
/// </summary>
public readonly record struct FontProp(string? Name, DoubleProp Size)
    : IProperty<FontProp>
{
    /// <summary>
    /// Indicates whether both fields are non-null.
    /// </summary>
    public bool IsComplete => Name != null && Size.HasValue;

    /// <summary>
    /// Returns this value, or the other only if this is null (for each component).
    /// </summary>
    public FontProp Or(FontProp other)
        => new FontProp(Name ?? other.Name, Size.Or(other.Size));

    /// <summary>
    /// Interpolates from this value to the destination. Returns the destination immediately if either is null.
    /// </summary>
    public FontProp Interpolate(FontProp destination, double percent)
    {
        return new FontProp(
            Name ?? destination.Name, // FontName cannot be interpolated, so use the destination if null
            Size.Interpolate(destination.Size, percent)
        );
    }

    /// <summary>
    /// Returns a string representation of the FontProp.
    /// </summary>
    public override string ToString()
        => $"Name: {Name ?? "null"}, Size: {Size}";

    /// <summary>
    /// Returns a formatted string representation of the FontProp.
    /// </summary>
    public string ToString(string format)
        => $"FontName: {Name ?? "null"}, FontSize: {Size.ToString(format)}";

    /// <summary>
    /// Implicit conversion from a tuple (string, double) to FontProp.
    /// </summary>
    public static implicit operator FontProp((string? FontName, double FontSize) value)
        => new FontProp(value.FontName, new DoubleProp(value.FontSize));

    /// <summary>
    /// Implicit conversion from a tuple (string, DoubleProp) to FontProp.
    /// </summary>
    public static implicit operator FontProp((string? FontName, DoubleProp FontSize) value)
        => new FontProp(value.FontName, value.FontSize);
}