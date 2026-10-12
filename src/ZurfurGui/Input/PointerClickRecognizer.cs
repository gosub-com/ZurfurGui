using ZurfurGui.Base;
using ZurfurGui.Controls;

namespace ZurfurGui.Input;

/// <summary>
/// Recognizes a press-and-release click on a <see cref="View"/>.
/// </summary>
/// <remarks>
/// Attach this utility to a view when the view needs click-like activation without exposing a routed
/// <c>PointerClick</c> event. The recognizer handles pointer down and up, captures the pointer while the press is
/// active, and invokes the supplied callback only when the pointer is released inside the view's bounds. Capture
/// loss, pointer cancellation cleanup, and release outside the bounds cancel the click. The callback receives the
/// pointer-up event and is responsible for the control-specific action, such as raising a public <c>Click</c> event
/// or selecting a ComboBox item.
///
/// The caller owns the recognizer and should dispose it when the attached view or control is no longer used.
/// </remarks>
public sealed class PointerClickRecognizer : IDisposable
{
    readonly View _view;
    readonly EventHandler<PointerEvent> _pointerDown;
    readonly EventHandler<PointerEvent> _pointerUp;
    readonly EventHandler _pointerCaptureLost;
    readonly EventHandler<PointerEvent> _clicked;
    bool _pressed;
    bool _disposed;

    /// <summary>
    /// Creates a press-and-release recognizer attached to <paramref name="view"/>.
    /// </summary>
    /// <param name="view">The view whose bounds define the activation area.</param>
    /// <param name="clicked">The callback invoked after a valid release inside the view's bounds.</param>
    public PointerClickRecognizer(View view, EventHandler<PointerEvent> clicked)
    {
        _view = view ?? throw new ArgumentNullException(nameof(view));
        _clicked = clicked ?? throw new ArgumentNullException(nameof(clicked));
        _pointerDown = OnPointerDown;
        _pointerUp = OnPointerUp;
        _pointerCaptureLost = OnPointerCaptureLost;

        _view.AddEvent(Panel.PointerDown, _pointerDown);
        _view.AddEvent(Panel.PointerUp, _pointerUp);
        _view.AddEvent(Panel.PointerCaptureLost, _pointerCaptureLost);
    }

    void OnPointerDown(object? sender, PointerEvent e)
    {
        if (_disposed || _pressed)
            return;

        _pressed = true;
        _view.CapturePointer = true;
        e.Handled = true;
    }

    void OnPointerUp(object? sender, PointerEvent e)
    {
        if (_disposed || !_pressed)
            return;

        e.Handled = true;
        var clicked = !e.Cancelled && _view.OriginRect.Contains(e.Contact.DevicePosition);
        _view.CapturePointer = false;
        _pressed = false;

        if (clicked)
            _clicked(_view, e);
    }

    void OnPointerCaptureLost(object? sender, EventArgs e)
    {
        _pressed = false;
    }

    /// <summary>
    /// Detaches the recognizer from its view and cancels any active press.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        if (_view.CapturePointer)
            _view.CapturePointer = false;

        _view.RemoveEvent(Panel.PointerDown, _pointerDown);
        _view.RemoveEvent(Panel.PointerUp, _pointerUp);
        _view.RemoveEvent(Panel.PointerCaptureLost, _pointerCaptureLost);
        _pressed = false;
    }
}
