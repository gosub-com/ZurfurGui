

namespace ZurfurGui.Input;


/// <summary>
/// Provides the pointer contact information used when determining whether a view is hit.
/// </summary>
public readonly record struct HitTestContext(PointerContact Contact);

/// <summary>
/// Specifies how a view participates in pointer hit testing.
/// </summary>
public enum HitTesting
{
    Normal,
    Disabled,
    Always
}
