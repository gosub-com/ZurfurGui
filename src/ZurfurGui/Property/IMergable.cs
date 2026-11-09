namespace ZurfurGui.Property;

/// <summary>
/// Allow mergable properties
/// </summary>
public interface IMergable<T> : IMergable, IEquatable<T>
{
    /// <summary>
    /// Return this value, or the other only if this is null (for each component).
    /// </summary>
    T Or(T other);

    object IMergable.OrObject(object other) => Or((T)other)!;
}

/// <summary>
/// Provides non-generic access to completeness checks and merging for property values.
/// </summary>
public interface IMergable
{
    /// <summary>
    /// Returns true if all components are non-null.
    /// </summary>
    bool IsComplete { get; }

    /// <summary>
    /// Combines this value with another value of the same type.
    /// </summary>
    object OrObject(object other);
}
