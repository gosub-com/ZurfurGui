using System;
using ZurfurGui.Base;

namespace ZurfurGui.Property;

/// <summary>
/// View background properties.
/// </summary>
public readonly record struct BackProp(
    ColorProp Color, ColorProp BorderColor, DoubleProp BorderWidth, DoubleProp Radius)
    : IProperty<BackProp>
{
    public BackProp() : this(new(), new(), new(), new()) { }

    public override string ToString() =>
        $"Color: {Color}, BorderColor: {BorderColor}, BorderWidth: {BorderWidth}, BorderRadius: {Radius}";

    public string ToString(string format) =>
        $"Color: {Color}, BorderColor: {BorderColor}, BorderWidth: {BorderWidth.ToString(format)}, BorderRadius: {Radius.ToString(format)}";

    /// <summary>
    /// Returns true if all components are non-null.
    /// </summary>
    public bool IsComplete => Color.HasValue && BorderColor.HasValue && BorderWidth.HasValue && Radius.HasValue;

    /// <summary>
    /// Return this value, or the other only if this is null (for each component).
    /// </summary>
    public BackProp Or(BackProp other) =>
        new BackProp(
            Color.Or(other.Color),
            BorderColor.Or(other.BorderColor),
            BorderWidth.Or(other.BorderWidth),
            Radius.Or(other.Radius)
        );

    /// <summary>
    /// Interpolate from this value to the destination.
    /// </summary>
    public BackProp Interpolate(BackProp destination, double percent)
    {
        return new BackProp(
            Color.Or(destination.Color), // Color interpolation is not defined, so use destination if null
            BorderColor.Or(destination.BorderColor), // Same for BorderColor
            BorderWidth.Interpolate(destination.BorderWidth, percent),
            Radius.Interpolate(destination.Radius, percent)
        );
    }
}
