namespace ZurfurGui.Property;

/// <summary>
/// Allow mergable properties
/// </summary>
public interface IMergable<T> : IEquatable<T>
{
    /// <summary>
    /// Returns true if all components are non-null.
    /// </summary>
    bool IsComplete { get; }

    /// <summary>
    /// Return this value, or the other only if this is null (for each component).
    /// </summary>
    T Or(T other);
}
