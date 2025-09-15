using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZurfurGui.Base;

/// <summary>
/// Type-safe ?double that never unwraps to NAN.  Defauts to null.
/// </summary>
public readonly struct DoubleProp : IEquatable<DoubleProp>
{
    public readonly double _value;

    public DoubleProp() => _value = double.NaN;

    public DoubleProp(double value) => _value = value;

    public bool Equals(DoubleProp other)
    {
        // Treat NaN as "null" and compare accordingly
        if (!HasValue && !other.HasValue)
            return true; 
        if (!HasValue || !other.HasValue)
            return false;
        return _value.Equals(other._value);
    }

    public override bool Equals(object? obj)
    {
        return obj is DoubleProp other && Equals(other);
    }

    public static bool operator ==(DoubleProp left, DoubleProp right) => left.Equals(right);
    public static bool operator !=(DoubleProp left, DoubleProp right) => !left.Equals(right);
    public override int GetHashCode() => HasValue ? _value.GetHashCode() : 0x12345678;
    public override string ToString() => HasValue ? _value.ToString() : "null";
    public string ToString(string format) => HasValue ? _value.ToString(format) : "null";

    /// <summary>
    /// Indicates whether the value is not NaN (i.e., not null).
    /// </summary>
    public bool HasValue => !double.IsNaN(_value);

    /// <summary>
    /// Gets the value or throws an InvalidOperationException if the value is null (NaN).
    /// </summary>
    public double Value
    {
        get
        {
            if (!HasValue)
                throw new InvalidOperationException("DoubleQ Value is null.");
            return _value;
        }
    }

    /// <summary>
    /// Return the value or def if null
    /// </summary>
    public double Or(double def) => HasValue ? _value : def;

    /// <summary>
    /// Gets the value or the specified default value if the value is null (NaN).
    /// </summary>
    public double GetValueOrDefault(double defaultValue = 0) => HasValue ? _value : defaultValue;

    /// <summary>
    /// Implicit conversion from double to DoubleQ. If value is NAN, it is converted to NULL.
    /// </summary>
    public static implicit operator DoubleProp(double value) => new DoubleProp(value);

    /// <summary>
    /// Implicit conversion from DoubleQ to double?.
    /// </summary>
    public static implicit operator double?(DoubleProp dq) => dq.HasValue ? dq._value : (double?)null;

    /// <summary>
    /// Implicit conversion from double? to DoubleQ.
    /// </summary>
    public static implicit operator DoubleProp(double? value) => value.HasValue ? new DoubleProp(value.Value) : new DoubleProp();
}

