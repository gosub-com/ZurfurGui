namespace ZurfurGui.Input;


public readonly record struct HitTestContext(PointerContact Contact);

public enum HitTesting
{
    Normal,
    Disabled,
    Always
}
