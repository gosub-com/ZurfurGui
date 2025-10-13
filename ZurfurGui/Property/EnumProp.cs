namespace ZurfurGui.Property;

/// <summary>
/// Type-safe nullable property that defaults to null.
/// </summary>
public readonly struct EnumProp<T> : IProperty<EnumProp<T>>
    where T : struct
{
    public readonly T? _value;

    public EnumProp(T value) => _value = value;

    public bool Equals(EnumProp<T> other)
    {
        // Compare nullable values
        return _value.Equals(other._value);
    }

    public override bool Equals(object? obj)
    {
        return obj is EnumProp<T> other && Equals(other);
    }

    public static bool operator ==(EnumProp<T> left, EnumProp<T> right) => left.Equals(right);
    public static bool operator !=(EnumProp<T> left, EnumProp<T> right) => !left.Equals(right);
    public override int GetHashCode() => _value?.GetHashCode() ?? 0x1234567A;
    public override string ToString() => _value?.ToString() ?? "null";

    /// <summary>
    /// Indicates whether the value is not null.
    /// </summary>
    public bool HasValue => _value.HasValue;

    /// <summary>
    /// Gets the value or throws an InvalidOperationException if the value is null.
    /// </summary>
    public T Value
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
    public EnumProp<T> Or(EnumProp<T> other)
        => HasValue ? this : other;

    /// <summary>
    /// Return this value, or the other only if this is null.
    /// </summary>
    public T Or(T other)
        => _value ?? other;

    /// <summary>
    /// Return destination immediately.
    /// </summary>
    public EnumProp<T> Interpolate(EnumProp<T> destination, double percent)
    {
        // Interpolation is not meaningful for most types, so return the destination.
        return destination;
    }

    /// <summary>
    /// Implicit conversion from T to GenericProp<T>.
    /// </summary>
    public static implicit operator EnumProp<T>(T value)
        => new EnumProp<T>(value);

    /// <summary>
    /// Implicit conversion from GenericProp<T> to T?.
    /// </summary>
    public static implicit operator T?(EnumProp<T> prop)
        => prop._value;

    /// <summary>
    /// Implicit conversion from T? to GenericProp<T>.
    /// </summary>
    public static implicit operator EnumProp<T>(T? value)
        => value.HasValue ? new EnumProp<T>(value.Value) : new EnumProp<T>();
}