using ZurfurGui.Base;
using ZurfurGui.Input;

namespace ZurfurGui.Platform;

/// <summary>
/// A platform wheel event. Deltas must already be converted to the browser's pixel-mode convention
/// (see <see cref="WheelEvent"/>), with positive Y scrolling down and positive X scrolling right.
/// </summary>
public record class OsWheelEvent(Point DevicePosition, double DeltaX, double DeltaY)
{
    public PointerButtons Buttons { get; init; }
    public PointerModifiers Modifiers { get; init; }
    public PointerDeviceKind Device { get; init; } = PointerDeviceKind.Mouse;
    public int ContactId { get; init; }

    /// <summary>
    /// Converts this platform event to the logical wheel event.
    /// </summary>
    public WheelEvent ToWheelEvent() => new(
        new PointerContact(
            Device,
            ContactId,
            DevicePosition,
            0,
            0,
            Buttons,
            PointerButtons.None,
            Modifiers),
        DeltaX,
        DeltaY);
}
