using System.Diagnostics;
using ZurfurGui.Base;
using ZurfurGui.Controls;
using ZurfurGui.Windows;

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
    readonly EventHandler<PointerEvent> _pointerMove;
    readonly EventHandler<PointerEvent> _pointerExited;
    readonly EventHandler<PointerContactKey> _pointerCaptureLost;
    readonly EventHandler<PointerEvent> _clicked;
    readonly EventHandler<PointerLongClickEventArgs>? _longClicked;
    Stopwatch? _longClickTimer;
    AppWindow? _longClickAppWindow;
    PointerContact _currentContact;
    Point _pressPosition;
    PointerContactKey? _contactKey;
    bool _pressed;
    bool _longClickHandled;
    bool _disposed;

    /// <summary>
    /// Creates a press-and-release recognizer attached to <paramref name="view"/>.
    /// </summary>
    /// <param name="view">The view whose bounds define the activation area.</param>
    /// <param name="clicked">The callback invoked after a valid release inside the view's bounds.</param>
    /// <param name="longClicked">Optional callback invoked after the configured long-click duration.</param>
    public PointerClickRecognizer(
        View view,
        EventHandler<PointerEvent> clicked,
        EventHandler<PointerLongClickEventArgs>? longClicked = null)
    {
        _view = view ?? throw new ArgumentNullException(nameof(view));
        _clicked = clicked ?? throw new ArgumentNullException(nameof(clicked));
        _longClicked = longClicked;
        _pointerDown = OnPointerDown;
        _pointerMove = OnPointerMove;
        _pointerExited = OnPointerExited;
        _pointerUp = OnPointerUp;
        _pointerCaptureLost = OnPointerCaptureLost;

        _view.AddEvent(Panel.PointerDown, _pointerDown);
        if (_longClicked is not null)
        {
            _view.AddEvent(Panel.PointerMove, _pointerMove);
            _view.AddEvent(Panel.PointerExited, _pointerExited);
        }
        _view.AddEvent(Panel.PointerUp, _pointerUp);
        _view.AddEvent(Panel.PointerCaptureLost, _pointerCaptureLost);
    }

    void OnPointerDown(object? sender, PointerEvent e)
    {
        if (_disposed || _pressed)
            return;

        _pressed = true;
        _contactKey = e.Contact.Key;
        _currentContact = e.Contact;
        _pressPosition = _view.toClient(e.Contact.DevicePosition);
        _longClickHandled = false;

        if (_longClicked is not null && _view.AppWindow is AppWindow appWindow)
        {
            _longClickAppWindow = appWindow;
            _longClickTimer = Stopwatch.StartNew();
            appWindow.PreRenderFrame += OnPreRenderFrame;
        }

        _view.SetCapture(e.Contact.Key, true);
        e.Handled = true;
    }

    void OnPointerMove(object? sender, PointerEvent e)
    {
        UpdateContact(e.Contact);
    }

    void OnPointerExited(object? sender, PointerEvent e)
    {
        UpdateContact(e.Contact);
    }

    void UpdateContact(PointerContact contact)
    {
        if (!_pressed || _contactKey != contact.Key)
            return;

        _currentContact = contact;

        if (_longClickAppWindow is null)
            return;

        var position = _view.toClient(contact.DevicePosition);
        var dx = position.X - _pressPosition.X;
        var dy = position.Y - _pressPosition.Y;
        var tolerance = _longClickAppWindow.Properties.LongClickMovementTolerance;
        if (dx * dx + dy * dy > tolerance * tolerance)
            StopLongClickTimer();
    }

    void OnPreRenderFrame(object? sender, EventArgs e)
    {
        Debug.Assert(!_disposed);
        if (_disposed || !_pressed || _longClicked is null || _longClickAppWindow is null || _longClickTimer is null)
            return;

        if (_longClickTimer.Elapsed < _longClickAppWindow.Properties.LongClickDuration)
            return;

        StopLongClickTimer();
        var args = new PointerLongClickEventArgs(_currentContact);
        _longClicked(_view, args);
        _longClickHandled = args.Handled;
    }

    void StopLongClickTimer()
    {
        if (_longClickAppWindow is AppWindow appWindow)
        {
            appWindow.PreRenderFrame -= OnPreRenderFrame;
        }

        _longClickAppWindow = null;
        _longClickTimer = null;
    }

    void OnPointerUp(object? sender, PointerEvent e)
    {
        if (_disposed || !_pressed || _contactKey != e.Contact.Key)
            return;

        e.Handled = true;
        var clicked = !_longClickHandled
            && !e.Cancelled
            && _view.OriginRect.Contains(e.Contact.DevicePosition);
        StopLongClickTimer();
        _view.SetCapture(e.Contact.Key, false);
        _pressed = false;
        _contactKey = null;

        if (clicked)
            _clicked(_view, e);
    }

    void OnPointerCaptureLost(object? sender, PointerContactKey contactKey)
    {
        if (_contactKey != contactKey)
            return;

        StopLongClickTimer();
        _pressed = false;
        _contactKey = null;
    }

    /// <summary>
    /// Detaches the recognizer from its view and cancels any active press.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        if (_contactKey is PointerContactKey contactKey && _view.AppWindow is not null)
            _view.SetCapture(contactKey, false);

        _view.RemoveEvent(Panel.PointerDown, _pointerDown);
        if (_longClicked is not null)
        {
            _view.RemoveEvent(Panel.PointerMove, _pointerMove);
            _view.RemoveEvent(Panel.PointerExited, _pointerExited);
        }
        _view.RemoveEvent(Panel.PointerUp, _pointerUp);
        _view.RemoveEvent(Panel.PointerCaptureLost, _pointerCaptureLost);
        StopLongClickTimer();
        _pressed = false;
        _contactKey = null;
    }
}
