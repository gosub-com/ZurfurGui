using System;

namespace ZurfurGui.Base;

/// <summary>
/// A Color property that allows the color to be optional.
/// TBD: Add separate alpha channel nullability.
/// Defaults to null.
/// </summary>
public readonly struct ColorProp : IEquatable<ColorProp>
{
    public readonly Color ?_color;

    public ColorProp() => _color = null;

    public ColorProp(Color value) => _color = value;

    public bool Equals(ColorProp other)
    {
        if (_color is null && other._color is null)
            return true;
        if (_color is null || other._color is null)
            return false;
        return _color.Value.Equals(other._color.Value);
    }

    public override bool Equals(object? obj)
    {
        return obj is ColorProp other && Equals(other);
    }

    public static bool operator ==(ColorProp left, ColorProp right) => left.Equals(right);
    public static bool operator !=(ColorProp left, ColorProp right) => !left.Equals(right);
    public override int GetHashCode() => HasValue ? _color.GetHashCode() : 0x12345679;
    public override string ToString() => _color != null ? _color.Value.ToString() : "null";

    /// <summary>
    /// Indicates whether the value is not null
    /// </summary>
    public bool HasValue => _color.HasValue;

    /// <summary>
    /// Gets the value or throws an InvalidOperationException if the value is null (NaN).
    /// </summary>
    public Color Value
    {
        get
        {
            return _color != null ? _color.Value : throw new InvalidOperationException("ColorProp Value is null.");
        }
    }

    /// <summary>
    /// Return the value or def if null
    /// </summary>
    public Color Or(Color def) => _color != null ? _color.Value : def;

    /// <summary>
    /// Gets the value or the specified default value if the value is null (NaN).
    /// </summary>
    public Color GetValueOrDefault(Color defaultValue) => _color != null ? _color.Value : defaultValue;

    /// <summary>
    /// Implicit conversion from Color to ColorProp.
    /// </summary>
    public static implicit operator ColorProp(Color value) => new ColorProp(value);

    /// <summary>
    /// Implicit conversion from ColorProp to Color?.
    /// </summary>
    public static implicit operator Color?(ColorProp color) => color._color != null ? color._color : null;

    /// <summary>
    /// Implicit conversion from Color? to ColorPtop.
    /// </summary>
    public static implicit operator ColorProp(Color? value) => value != null ? new ColorProp(value.Value) : new ColorProp();
}
