namespace ZurfurGui.Input;

/// <summary>
/// A mouse wheel or touchpad scroll event delivered to the view under the pointer.
/// </summary>
/// <remarks>
/// Deltas use the browser's pixel-mode convention, which is the reference for all platforms: one detent of a
/// typical mouse wheel is <see cref="UnitsPerNotch"/> units, and precision devices report fractions of that.
/// Positive <see cref="DeltaY"/> means scroll down (the content moves up) and positive <see cref="DeltaX"/> means
/// scroll right. The contact position is in device pixels, like <see cref="PointerEvent"/>.
/// </remarks>
public sealed record WheelEvent(PointerContact Contact, double DeltaX, double DeltaY)
{
    /// <summary>
    /// Number of delta units reported for one detent of a typical mouse wheel.
    /// </summary>
    public const double UnitsPerNotch = 100;

    /// <summary>
    /// Set by a handler to consume the wheel. Until a handler sets this, the router continues
    /// offering the event to views closer to the root.
    /// </summary>
    public bool Handled { get; set; }

    /// <summary>
    /// Gets the horizontal delta in detents.
    /// </summary>
    public double NotchesX => DeltaX / UnitsPerNotch;

    /// <summary>
    /// Gets the vertical delta in detents.
    /// </summary>
    public double NotchesY => DeltaY / UnitsPerNotch;
}
