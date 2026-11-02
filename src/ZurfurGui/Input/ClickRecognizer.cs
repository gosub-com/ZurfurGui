using System.Diagnostics;
using ZurfurGui.Base;
using ZurfurGui.Windows;

namespace ZurfurGui.Input;

/// <summary>
/// Recognizes a click on a view as part of the shared gesture arbitration system.
/// </summary>
/// <remarks>
/// The click remains a candidate while the pointer is down. It invokes the supplied callback if the pointer is
/// released inside the view and no other gesture wins first. Attach it with
/// <see cref="View.AddGestureRecognizer{T}"/> and detach it with <see cref="View.RemoveGestureRecognizer"/>.
/// </remarks>
public sealed class ClickRecognizer : IGestureRecognizer
{
    readonly EventHandler<PointerEvent> _clicked;
    readonly EventHandler<PointerLongClickEventArgs>? _longClicked;
    readonly PointerButtons _mouseButtons;

    /// <summary>
    /// Creates a click recognizer. The attached view's bounds define the activation area.
    /// </summary>
    /// <param name="clicked">The callback invoked after a valid release inside the view's bounds.</param>
    /// <param name="longClicked">Optional callback invoked after the configured long-click duration.</param>
    /// <param name="mouseButtons">
    /// The mouse buttons that click (default is left).  Touch and pen are not affected.
    /// Use <see cref="PointerButtons.None"/> to prevent the mouse from clicking.
    /// </param>
    public ClickRecognizer(
        EventHandler<PointerEvent> clicked,
        EventHandler<PointerLongClickEventArgs>? longClicked = null,
        PointerButtons mouseButtons = PointerButtons.Left)
    {
        ArgumentNullException.ThrowIfNull(clicked);
        _clicked = clicked;
        _longClicked = longClicked;
        _mouseButtons = mouseButtons;
    }

    /// <summary>
    /// Creates the click state for one contact, or null if the contact is a mouse button that
    /// does not click.
    /// </summary>
    public IGestureRecognizerSession? CreateSession(GestureSessionContext context) =>
        AcceptsContact(context.Contact) ? new ClickSession(context, _clicked, _longClicked) : null;

    // Returns true if the device and button of the contact are allowed to click.
    // Note: PanRecognizer.AcceptsContact has similar code.
    bool AcceptsContact(PointerContact contact)
    {
        if (contact.Device == PointerDeviceKind.Touch || contact.Device == PointerDeviceKind.Pen)
            return true;
        var button = contact.ChangedButton != PointerButtons.None ? contact.ChangedButton : contact.Buttons;
        return (button & _mouseButtons) != 0;
    }

    // Tracks one contact: click on release inside the view, or long-click after a hold.
    sealed class ClickSession : IGestureRecognizerSession
    {
        readonly GestureSessionContext _context;
        readonly EventHandler<PointerEvent> _clicked;
        readonly EventHandler<PointerLongClickEventArgs>? _longClicked;
        readonly Point _pressPosition;
        Stopwatch? _longClickTimer;
        AppWindow? _longClickAppWindow;
        PointerContact _currentContact;
        bool _longClickHandled;

        public ClickSession(
            GestureSessionContext context,
            EventHandler<PointerEvent> clicked,
            EventHandler<PointerLongClickEventArgs>? longClicked)
        {
            _context = context;
            _clicked = clicked;
            _longClicked = longClicked;
            _currentContact = context.Contact;
            _pressPosition = context.Contact.DevicePosition;
        }

        public void OnPointerDown(PointerEvent e)
        {
            if (_longClicked is null || _context.CandidateView.AppWindow is not AppWindow appWindow)
                return;

            _longClickAppWindow = appWindow;
            _longClickTimer = Stopwatch.StartNew();
            appWindow.PreRenderFrame += OnPreRenderFrame;
        }

        public void OnPointerMove(PointerEvent e)
        {
            _currentContact = e.Contact;
            if (_longClickAppWindow is null)
                return;

            var position = e.Contact.DevicePosition;
            var dx = position.X - _pressPosition.X;
            var dy = position.Y - _pressPosition.Y;
            var tolerance = _longClickAppWindow.GetMovementSlop(e.Contact.Device);
            if (dx * dx + dy * dy > tolerance * tolerance)
                StopLongClickTimer();
        }

        public void OnPointerUp(PointerEvent e)
        {
            StopLongClickTimer();
            if (!_longClickHandled && !e.Cancelled &&
                _context.CandidateView.OriginRect.Contains(e.Contact.DevicePosition))
            {
                _context.Accept();
                _clicked(_context.CandidateView, e);
            }
            else
            {
                _context.Reject();
            }
        }

        public void OnCancelled()
        {
            StopLongClickTimer();
        }

        void OnPreRenderFrame(object? sender, EventArgs e)
        {
            if (_longClickAppWindow is null || _longClickTimer is null || _longClicked is null)
                return;

            if (_longClickTimer.Elapsed < _longClickAppWindow.Properties.LongClickDuration)
                return;

            StopLongClickTimer();
            var args = new PointerLongClickEventArgs(_currentContact);
            _longClicked(_context.CandidateView, args);
            _longClickHandled = args.Handled;
            if (_longClickHandled)
                _context.Accept();
        }

        void StopLongClickTimer()
        {
            if (_longClickAppWindow is AppWindow appWindow)
                appWindow.PreRenderFrame -= OnPreRenderFrame;

            _longClickAppWindow = null;
            _longClickTimer = null;
        }
    }
}
