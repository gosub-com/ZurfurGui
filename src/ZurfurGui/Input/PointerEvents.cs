using ZurfurGui.Base;



namespace ZurfurGui.Input;

/// <summary>
/// Identifies the buttons currently pressed on a pointer device.
/// </summary>
[Flags]
public enum PointerButtons : byte
{
    None = 0,
    Left = 1,
    Right = 2,
    Middle = 4,
    Back = 8,
    Forward = 16
}

/// <summary>
/// Identifies keyboard modifiers active during a pointer interaction.
/// </summary>
[Flags]
public enum PointerModifiers : byte
{
    None = 0,
    Shift = 1,
    Control = 2,
    Alt = 4,
    Meta = 8
}

/// <summary>
/// Identifies the kind of device that produced a pointer contact.
/// </summary>
public enum PointerDeviceKind : byte
{
    Unknown,
    Mouse,
    Touch,
    Pen
}

/// <summary>
/// Identifies the kind of pointer event reported for a contact.
/// </summary>
public enum PointerEventKind : byte
{
    Unknown,
    Move,
    Down,
    Up,
    Enter,
    Leave,
    Cancel
}

/// <summary>
/// Describes the current state and position of a pointer contact.
/// </summary>
public readonly record struct PointerContact(
    PointerDeviceKind Device,
    int Id,
    Point DevicePosition,
    double Width,
    double Height,
    PointerButtons Buttons,
    PointerButtons ChangedButton,
    PointerModifiers Modifiers)
{
    public PointerContactKey Key => new(Device, Id);
}

/// <summary>
/// Uniquely identifies a pointer contact by its device and device-specific identifier.
/// </summary>
public readonly record struct PointerContactKey(PointerDeviceKind Device, int Id);

/// <summary>
/// Provides the latest contact state to a long-click callback and indicates whether it was handled.
/// </summary>
public sealed class PointerLongClickEventArgs : EventArgs
{
    /// <summary>
    /// Gets the latest state of the pointer contact that triggered the long click.
    /// </summary>
    public PointerContact Contact { get; }

    /// <summary>
    /// Gets or sets whether the long click was handled and the ordinary click should be suppressed.
    /// </summary>
    public bool Handled { get; set; }

    /// <summary>
    /// Creates event arguments for a long-click callback.
    /// </summary>
    public PointerLongClickEventArgs(PointerContact contact)
    {
        Contact = contact;
    }
}

/// <summary>
/// A pointer contact together with the view that currently owns its capture.
/// </summary>
public sealed record CapturedPointerContact(PointerContact Contact, View CapturedView);

/// <summary>
/// Use View.ToClinet to convert DevicePosition to view position.
/// </summary>
public record class PointerEvent(PointerEventKind Kind)
{
    public PointerContact Contact { get; init; }

    /// <summary>
    /// The event time in milliseconds from a monotonic clock.  The platform supplies it when it can,
    /// otherwise the router stamps it on arrival.  Only differences between events of the same
    /// contact are meaningful.
    /// </summary>
    public double Timestamp { get; init; }

    /// <summary>
    /// Gets or sets whether normal routed event processing has handled this event.
    /// </summary>
    /// <remarks>
    /// Handled stops normal routing at the current route boundary. During preview routing it also prevents the
    /// corresponding bubble route from running. Handling an event does not cancel the control's own interaction;
    /// for example, a button can handle its pointer-down event and continue tracking the press for a later click.
    /// </remarks>
    public bool Handled { get; set; }

    /// <summary>
    /// Gets or sets whether this pointer interaction has been cancelled.
    /// </summary>
    /// <remarks>
    /// Cancellation stops normal routing for the current event rather than merely marking it handled. Controls should
    /// also release capture or use the router's subtree-cancellation operation when the underlying interaction is no
    /// longer valid. The pointer router performs the associated capture and pressed-state cleanup for pointer cancel
    /// events and explicit subtree cancellation.
    /// </remarks>
    public bool Cancelled { get; set; }
}
