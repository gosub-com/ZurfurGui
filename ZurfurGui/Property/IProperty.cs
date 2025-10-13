namespace ZurfurGui.Property;

public interface IProperty<T> : IEquatable<T> where T : new()
{
    /// <summary>
    /// Returns true if all components are non-null.
    /// </summary>
    bool IsComplete { get; }

    /// <summary>
    /// Return this value, or the other only if this is null (for each component).
    /// </summary>
    T Or(T other);

    /// <summary>
    /// Interpolate from this value to the destination.  
    /// Things that aren't interpolatable (including either being null) return destination immediately.
    /// </summary>
    T Interpolate(T destination, double percent) => destination;
}
