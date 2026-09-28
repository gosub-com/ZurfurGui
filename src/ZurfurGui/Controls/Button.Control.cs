using ZurfurGui.Base;
using ZurfurGui.Input;

namespace ZurfurGui.Controls;

/// <summary>
/// A clickable button control. Subscribe to <see cref="Click"/> for button activation instead of using low-level pointer
/// events.
/// </summary>
public partial class Button : Controllable
{
    public event EventHandler? Click;

    public Button()
    {
        InitializeControl();
        View.AddEvent(Panel.PointerClick, OnPointerClick);
    }

    private void OnPointerClick(object? sender, PointerEvent e)
    {
        Click?.Invoke(this, EventArgs.Empty);
    }
}
