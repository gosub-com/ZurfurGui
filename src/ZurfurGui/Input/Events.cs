using ZurfurGui.Base;

namespace ZurfurGui.Input;

[Flags]
public enum PointerButtons
{
    None = 0,
    Left = 1,
    Right = 2,
    Middle = 4,
    Back = 8,
    Forward = 16
}

[Flags]
public enum PointerModifiers
{
    None = 0,
    Shift = 1,
    Control = 2,
    Alt = 4,
    Meta = 8
}

public enum PointerDeviceKind
{
    Unknown,
    Mouse,
    Touch,
    Pen
}

public enum PointerEventKind
{
    Unknown,
    Move,
    Down,
    Up,
    Enter,
    Leave,
    Cancel
}

public readonly record struct PointerContact(
    PointerDeviceKind Device,
    int Id,
    Point DevicePosition,
    double Width,
    double Height,
    PointerButtons Buttons,
    PointerButtons ChangedButton,
    PointerModifiers Modifiers);

/// <summary>
/// Use View.ToClinet to convert DevicePosition to view position.
/// </summary>
public record class PointerEvent(PointerEventKind Kind)
{
    public PointerContact Contact { get; init; }

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
