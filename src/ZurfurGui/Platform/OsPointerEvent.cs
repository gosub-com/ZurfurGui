using ZurfurGui.Base;
using ZurfurGui.Input;

namespace ZurfurGui.Platform;

public record class OsPointerEvent(string Type, Point DevicePosition)
{
    public PointerButtons Buttons { get; init; }
    public PointerButtons ChangedButton { get; init; }
    public PointerModifiers Modifiers { get; init; }
    public PointerDeviceKind Device { get; init; } = PointerDeviceKind.Mouse;
    public int ContactId { get; init; }
    public double ContactWidth { get; init; }
    public double ContactHeight { get; init; }

    public PointerEvent ToPointerEvent() => new(ParseKind(Type))
    {
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
        "pointerclick" => PointerEventKind.Click,
        _ => PointerEventKind.Unknown
    };
}
