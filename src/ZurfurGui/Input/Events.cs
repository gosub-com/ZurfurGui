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
    Cancel,
    Click
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
}
