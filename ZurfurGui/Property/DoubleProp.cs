namespace ZurfurGui.Property;

public readonly struct DoubleProp : IProperty<DoubleProp>
{
    readonly double? _value;

    public DoubleProp(double value) => _value = value;

    public bool Equals(DoubleProp other)
    {
        return _value == other._value;
    }

    public override bool Equals(object? obj)
    {
        return obj is DoubleProp other && Equals(other);
    }

    public static bool operator ==(DoubleProp left, DoubleProp right) => left.Equals(right);
    public static bool operator !=(DoubleProp left, DoubleProp right) => !left.Equals(right);
    public override int GetHashCode() => _value.GetHashCode();
    public override string ToString() => _value != null ? _value.Value.ToString() : "null";
    public string ToString(string format) => _value != null ? _value.Value.ToString(format) : "null";

    /// <summary>
    /// Indicates whether the value is not NaN (i.e., not null).
    /// </summary>
    public bool HasValue => _value.HasValue;

    /// <summary>
    /// Gets the value or throws an InvalidOperationException if the value is null (NaN).
    /// </summary>
    public double Value
    {
        get
        {
            if (_value == null)
                throw new InvalidOperationException("DoubleProp Value is null.");
            return _value.Value;
        }
    }

    /// <summary>
    /// Returns true if non-null.
    /// </summary>
    public bool IsComplete => HasValue;

    /// <summary>
    /// Return this value, or the other only if this is null.
    /// </summary>
    public DoubleProp Or(DoubleProp other) 
        => HasValue ? this : other;

    /// <summary>
    /// Return this value, or the other only if this is null.
    /// </summary>
    public double Or(double other) 
        => _value != null ? _value.Value : other;

    /// <summary>
    /// Interpolate from this value to the destination.  Return destination immediately if either is null.
    /// </summary>
    public DoubleProp Interpolate(DoubleProp destination, double percent)
    {
        if (_value == null || destination == null)
            return destination;
        var s = _value.Value;
        var d = destination.Value;
        return new DoubleProp(s + (d - s) * percent);
    }

    /// <summary>
    /// Implicit conversion from double to DoubleQ. If value is NAN, it is converted to NULL.
    /// </summary>
    public static implicit operator DoubleProp(double value) 
        => new DoubleProp(value);

    /// <summary>
    /// Implicit conversion from DoubleQ to double?.
    /// </summary>
    public static implicit operator double?(DoubleProp dq) 
        => dq.HasValue ? dq._value : null;

    /// <summary>
    /// Implicit conversion from double? to DoubleQ.
    /// </summary>
    public static implicit operator DoubleProp(double? value) 
        => value.HasValue ? new DoubleProp(value.Value) : new DoubleProp();
}

