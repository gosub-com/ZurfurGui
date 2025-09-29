namespace ZurfurGui.Property;

/// <summary>
/// Type-safe ?bool that defaults to null.
/// </summary>
public readonly struct BoolProp : IProperty<BoolProp>
{
    public readonly bool? _value;

    public BoolProp() => _value = null;

    public BoolProp(bool value) => _value = value;

    public bool Equals(BoolProp other)
    {
        // Compare nullable bool values
        return _value == other._value;
    }

    public override bool Equals(object? obj)
    {
        return obj is BoolProp other && Equals(other);
    }

    public static bool operator ==(BoolProp left, BoolProp right) => left.Equals(right);
    public static bool operator !=(BoolProp left, BoolProp right) => !left.Equals(right);
    public override int GetHashCode() => _value?.GetHashCode() ?? 0x1234567A;
    public override string ToString() => _value?.ToString() ?? "null";

    /// <summary>
    /// Indicates whether the value is not null.
    /// </summary>
    public bool HasValue => _value.HasValue;

    /// <summary>
    /// Gets the value or throws an InvalidOperationException if the value is null.
    /// </summary>
    public bool Value
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
    public BoolProp Or(BoolProp other)
        => HasValue ? this : other;

    /// <summary>
    /// Return this value, or the other only if this is null.
    /// </summary>
    public bool Or(bool other)
        => _value ?? other;

    /// <summary>
    /// Return destination immediately.
    /// </summary>
    public BoolProp Interpolate(BoolProp destination, double percent)
    {
        return destination;
    }

    /// <summary>
    /// Implicit conversion from bool to BoolProp.
    /// </summary>
    public static implicit operator BoolProp(bool value)
        => new BoolProp(value);

    /// <summary>
    /// Implicit conversion from BoolProp to bool?
    /// </summary>
    public static implicit operator bool?(BoolProp bp)
        => bp._value;

    /// <summary>
    /// Implicit conversion from bool? to BoolProp.
    /// </summary>
    public static implicit operator BoolProp(bool? value)
        => value.HasValue ? new BoolProp(value.Value) : new BoolProp();
}
