using System.Runtime.CompilerServices;
using ZurfurGui.Base;

namespace ZurfurGui.Property;

/// <summary>
/// Type-safe ?TextLines that defaults to null.
/// </summary>
[CollectionBuilder(typeof(TextLinesPropBuilder), nameof(TextLinesPropBuilder.Create))]
public readonly struct TextLinesProp : IProperty<TextLinesProp>
{
    public readonly TextLines? _value;

    public TextLinesProp() => _value = null;

    public TextLinesProp(ReadOnlySpan<string> values) => _value = new TextLines(values);
    public TextLinesProp(string value) => _value = new TextLines(value);

    public bool Equals(TextLinesProp other)
    {
        return _value == other._value;
    }

    public override bool Equals(object? obj)
    {
        return obj is TextLinesProp other && Equals(other);
    }

    public static bool operator ==(TextLinesProp left, TextLinesProp right) => left.Equals(right);
    public static bool operator !=(TextLinesProp left, TextLinesProp right) => !left.Equals(right);
    public override int GetHashCode() => _value?.GetHashCode() ?? 0x1234567A;
    public override string ToString() => _value?.ToString() ?? "null";

    /// <summary>
    /// Indicates whether the value is not null.
    /// </summary>
    public bool HasValue => _value != null;

    /// <summary>
    /// Gets the value or throws an InvalidOperationException if the value is null.
    /// </summary>
    public TextLines Value
    {
        get
        {
            return _value ?? throw new InvalidOperationException();
        }
    }

    /// <summary>
    /// Returns true if non-null.
    /// </summary>
    public bool IsComplete => HasValue;

    /// <summary>
    /// Return this value, or the other only if this is null.
    /// </summary>
    public TextLinesProp Or(TextLinesProp other)
        => HasValue ? this : other;

    /// <summary>
    /// Return this value, or the other only if this is null.
    /// </summary>
    public TextLines Or(TextLines other)
        => _value ?? other;

    /// <summary>
    /// Return destination immediately.
    /// </summary>
    public TextLinesProp Interpolate(TextLinesProp destination, double percent)
    {
        return destination;
    }


    /// <summary>
    /// Builder for TextLinesProp collections.
    /// </summary>
    internal static class TextLinesPropBuilder
    {
        internal static TextLinesProp Create(ReadOnlySpan<string> values)
        {
            return new TextLinesProp(values);
        }
    }
}

