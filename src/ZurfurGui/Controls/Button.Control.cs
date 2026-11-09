using ZurfurGui.Base;
using ZurfurGui.Input;

namespace ZurfurGui.Controls;

/// <summary>
/// A clickable button control. Subscribe to <see cref="Click"/> for button activation instead of using low-level pointer
/// events.
/// </summary>
public partial class Button : Controllable
{
    /// <summary>
    /// Occurs when the button is activated by a click.
    /// </summary>
    public event EventHandler? Click;

    /// <summary>
    /// Initializes a button and registers its click gesture handler.
    /// </summary>
    public Button()
    {
        InitializeControl();
        View.AddGestureRecognizer(new ClickRecognizer(OnPointerClick));
    }

    private void OnPointerClick(object? sender, PointerEvent e)
    {
        Click?.Invoke(this, EventArgs.Empty);
    }
}
