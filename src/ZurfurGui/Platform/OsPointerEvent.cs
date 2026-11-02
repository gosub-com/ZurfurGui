using System.Diagnostics;
using ZurfurGui.Base;
using ZurfurGui.Input;

namespace ZurfurGui.Platform;

internal sealed record class OsPointerEvent(string Type, Point DevicePosition)
{
    // Fallback clock for platforms that supply no event time.
    static readonly Stopwatch s_clock = Stopwatch.StartNew();

    /// <summary>
    /// The platform event time in milliseconds from a monotonic clock, or zero if the platform does not
    /// supply one (the router then stamps the event with its own clock).  All events of one contact
    /// should come from the same clock.
    /// </summary>
    public double Timestamp { get; init; }

    public PointerButtons Buttons { get; init; }
    public PointerButtons ChangedButton { get; init; }
    public PointerModifiers Modifiers { get; init; }
    public PointerDeviceKind Device { get; init; } = PointerDeviceKind.Mouse;
    public int ContactId { get; init; }
    public double ContactWidth { get; init; }
    public double ContactHeight { get; init; }

    public PointerEvent ToPointerEvent() => new(ParseKind(Type))
    {
        Timestamp = Timestamp > 0 ? Timestamp : s_clock.Elapsed.TotalMilliseconds,
        Contact = new(
            Device,
            ContactId,
            DevicePosition,
            ContactWidth,
            ContactHeight,
            Buttons,
            ChangedButton,
            Modifiers)
    };

    static PointerEventKind ParseKind(string type) => type switch
    {
        "pointermove" => PointerEventKind.Move,
        "pointerdown" => PointerEventKind.Down,
        "pointerup" => PointerEventKind.Up,
        "pointerenter" => PointerEventKind.Enter,
        "pointerleave" => PointerEventKind.Leave,
        "pointercancel" => PointerEventKind.Cancel,
        _ => PointerEventKind.Unknown
    };
}
