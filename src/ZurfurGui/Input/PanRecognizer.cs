using ZurfurGui.Base;
using ZurfurGui.Windows;



namespace ZurfurGui.Input;

/// <summary>
/// The directions in which a pan recognizer responds to movement.
/// </summary>
[Flags]
public enum PanAxes
{
    None = 0,
    Horizontal = 1,
    Vertical = 2,
    Both = Horizontal | Vertical
}

/// <summary>
/// Configures a <see cref="PanRecognizer"/>.  The options can be changed at any time; a change applies
/// to contacts that start afterwards (and to the axes of a pan already in progress).
/// </summary>
public sealed class PanOptions
{
    /// <summary>
    /// The directions the recognizer responds to.  Movement along other axes does not start a pan and is
    /// not reported.  With <see cref="PanAxes.None"/> the recognizer does not take part in arbitration,
    /// so an ancestor recognizer can pan instead.
    /// </summary>
    public PanAxes Axes { get; set; } = PanAxes.Both;

    /// <summary>
    /// Distance in layout units (scaled by the DPR) a contact must move from where it went down before
    /// the pan starts.  Null uses the app window's movement slop for the device.  Use zero (or about
    /// one) for controls that should pan immediately.
    /// </summary>
    public double? Slop { get; set; }

    /// <summary>Whether a touch contact pans.</summary>
    public bool TouchPans { get; set; } = true;

    /// <summary>Whether a pen contact pans.</summary>
    public bool PenPans { get; set; } = true;

    /// <summary>
    /// The mouse buttons that pan.  The default is the right button, so a left click and drag can still
    /// select text or drag a thumb.  Set to <see cref="PointerButtons.Left"/> to pan with the left button.
    /// Use <see cref="PointerButtons.None"/> to prevent the mouse from panning.
    /// </summary>
    public PointerButtons MouseButtons { get; set; } = PointerButtons.Right;

    /// <summary>
    /// Whether a touch or pen contact reports a release velocity so the owner can continue the pan after
    /// the contact lifts.  The mouse never flicks.
    /// </summary>
    public bool Flick { get; set; } = true;

    /// <summary>
    /// A release slower than this (device pixels per second) is not a flick and reports zero velocity.
    /// </summary>
    public double MinFlickVelocity { get; set; } = 50;

    /// <summary>
    /// The release velocity (device pixels per second) is limited to this magnitude.
    /// </summary>
    public double MaxFlickVelocity { get; set; } = 8000;
}

/// <summary>
/// Describes a pan notification.  Which values are meaningful depends on the event, and all distances
/// are in device pixels.  Only the axes enabled in <see cref="PanOptions.Axes"/> are reported; the rest
/// are zero.
/// </summary>
public sealed class PanEventArgs : EventArgs
{
    /// <summary>The contact that is panning.</summary>
    public PointerContact Contact { get; init; }

    /// <summary>
    /// For <see cref="PanRecognizer.PanMoved"/>, the movement since the previous pan event.  It is zero in
    /// <see cref="PanRecognizer.PanStarted"/>, so the content does not jump when the pan begins.
    /// </summary>
    public Point Delta { get; init; }

    /// <summary>
    /// For <see cref="PanRecognizer.PanStarted"/>, the movement from where the contact went down to where
    /// the pan started (the slop distance and a little more).  Apply it to jump to the finger, or ignore
    /// it to start without a jump.
    /// </summary>
    public Point SlopDelta { get; init; }

    /// <summary>
    /// For <see cref="PanRecognizer.PanEnded"/>, the release velocity in device pixels per second.  It is
    /// zero for a mouse, when flick is off, when the release was too slow, or when cancelled.
    /// </summary>
    public Point Velocity { get; init; }

    /// <summary>For <see cref="PanRecognizer.PanEnded"/>, whether the pan was cancelled instead of released.</summary>
    public bool Cancelled { get; init; }
}

/// <summary>
/// Recognizes panning (dragging) as part of the shared gesture arena.  It knows nothing about what is
/// being panned or its bounds; the owner applies the movement it reports and decides what to do with the
/// release velocity.
/// </summary>
/// <remarks>
/// The recognizer stays pending until the contact moves farther than the slop along an enabled axis, then
/// accepts, which cancels competing recognizers (such as a click on a child) and captures the contact for
/// its view.  A contact released before that rejects, so a click can still win.  Attach it with
/// <see cref="View.AddGestureRecognizer{T}"/>.
/// </remarks>
public sealed class PanRecognizer : IGestureRecognizer
{
    /// <summary>Gets the options.  They may be changed while the recognizer is attached.</summary>
    public PanOptions Options { get; }

    /// <summary>
    /// Optional callback asked at pointer-down whether the owner's content is still moving from an earlier
    /// pan (a flick).  If it returns true, the pan starts immediately, with no slop, so the touch stops
    /// the motion and does not also click whatever is under the finger.
    /// </summary>
    public Func<bool>? CatchOnDown { get; set; }

    /// <summary>Raised once when the pan starts, after the contact moved beyond the slop.</summary>
    public event EventHandler<PanEventArgs>? PanStarted;

    /// <summary>Raised for each movement after the pan started.</summary>
    public event EventHandler<PanEventArgs>? PanMoved;

    /// <summary>Raised when a started pan is released or cancelled.</summary>
    public event EventHandler<PanEventArgs>? PanEnded;

    /// <summary>Creates a pan recognizer with default options, or with the options supplied.</summary>
    public PanRecognizer(PanOptions? options = null)
    {
        Options = options ?? new PanOptions();
    }

    /// <summary>
    /// Creates the pan state for one contact, or null if the recognizer does not apply to the contact's
    /// device and button or has no enabled axes.
    /// </summary>
    public IGestureRecognizerSession? CreateSession(GestureSessionContext context)
    {
        if (Options.Axes == PanAxes.None || !AcceptsContact(context.Contact))
            return null;

        return new PanSession(this, context);
    }

    // Returns true if the device and button of the contact are allowed to pan.
    // Note: ClickRecognizer.AcceptsContact has similar code.
    bool AcceptsContact(PointerContact contact)
    {
        switch (contact.Device)
        {
            case PointerDeviceKind.Touch:
                return Options.TouchPans;
            case PointerDeviceKind.Pen:
                return Options.PenPans;
            default:
                var button = contact.ChangedButton != PointerButtons.None ? contact.ChangedButton : contact.Buttons;
                return (button & Options.MouseButtons) != 0;
        }
    }

    // Zeroes the axes that are not enabled.
    static Point MaskAxes(Point p, PanAxes axes)
    {
        return new Point(
            (axes & PanAxes.Horizontal) != 0 ? p.X : 0,
            (axes & PanAxes.Vertical) != 0 ? p.Y : 0);
    }

    // Tracks one contact from pointer-down until it pans, is rejected, or is cancelled.
    sealed class PanSession : IGestureRecognizerSession
    {
        readonly PanRecognizer _owner;
        readonly GestureSessionContext _context;
        readonly VelocityTracker _velocity = new();
        Point _downPosition;
        Point _lastPosition;
        bool _started;
        bool _finished;

        public PanSession(PanRecognizer owner, GestureSessionContext context)
        {
            _owner = owner;
            _context = context;
            _downPosition = context.Contact.DevicePosition;
            _lastPosition = _downPosition;
        }

        public void OnPointerDown(PointerEvent e)
        {
            _downPosition = e.Contact.DevicePosition;
            _lastPosition = _downPosition;
            _velocity.Add(e.Timestamp, _downPosition);

            if (_owner.CatchOnDown?.Invoke() == true)
                Start(e.Contact, default);
        }

        public void OnPointerMove(PointerEvent e)
        {
            if (_finished)
                return;

            var position = e.Contact.DevicePosition;
            _velocity.Add(e.Timestamp, position);
            if (!_started)
                TryStart(e, position);
            else
                ReportMove(e.Contact, position);
        }

        public void OnPointerUp(PointerEvent e)
        {
            if (_finished)
                return;

            if (!_started)
            {
                _finished = true;
                _context.Reject();
                return;
            }

            if (e.Cancelled)
            {
                End(e.Contact, default, true);
                return;
            }

            ReportMove(e.Contact, e.Contact.DevicePosition);
            End(e.Contact, GetReleaseVelocity(e), false);
        }

        public void OnCancelled()
        {
            if (_finished)
                return;

            if (_started)
                End(_context.Contact, default, true);
            _finished = true;
        }

        // Starts the pan once the movement from the down point exceeds the slop along an enabled axis.
        void TryStart(PointerEvent e, Point position)
        {
            var displacement = MaskAxes(
                new Point(position.X - _downPosition.X, position.Y - _downPosition.Y), _owner.Options.Axes);
            var slop = GetSlop(e.Contact.Device);
            if (displacement.X * displacement.X + displacement.Y * displacement.Y <= slop * slop)
                return;

            // Accepting resolves the arena synchronously.  If another candidate already won, this session
            // was cancelled, so there is no pan to start.
            Start(e.Contact, displacement);
        }

        // Accepts the gesture and reports the start of the pan.  The pan moves relative to this position.
        void Start(PointerContact contact, Point slopDelta)
        {
            _context.Accept();
            if (_finished)
                return;

            _started = true;
            _lastPosition = contact.DevicePosition;
            _owner.PanStarted?.Invoke(_owner, new PanEventArgs { Contact = contact, SlopDelta = slopDelta });
        }

        // Reports the movement since the last report, if any.
        void ReportMove(PointerContact contact, Point position)
        {
            var delta = MaskAxes(
                new Point(position.X - _lastPosition.X, position.Y - _lastPosition.Y), _owner.Options.Axes);
            _lastPosition = position;
            if (delta.X == 0 && delta.Y == 0)
                return;

            _owner.PanMoved?.Invoke(_owner, new PanEventArgs { Contact = contact, Delta = delta });
        }

        // Finishes a started pan and reports it.
        void End(PointerContact contact, Point velocity, bool cancelled)
        {
            _finished = true;
            _owner.PanEnded?.Invoke(_owner, new PanEventArgs
            {
                Contact = contact,
                Velocity = velocity,
                Cancelled = cancelled
            });
        }

        // Returns the movement (device pixels) required to start a pan for the device.
        double GetSlop(PointerDeviceKind device)
        {
            if (_owner.Options.Slop is double slop)
                return Math.Max(0, slop) * (_context.CandidateView.AppWindow?.DevicePixelRatio ?? 1);

            var appWindow = _context.CandidateView.AppWindow;
            return appWindow?.GetMovementSlop(device) ?? 0;
        }

        // Returns the release velocity in device pixels per second.  Mouse contacts, and contacts when flick
        // is off, never flick.  A velocity below the minimum is not a flick, and the maximum is a limit.
        Point GetReleaseVelocity(PointerEvent e)
        {
            var options = _owner.Options;
            if (!options.Flick || e.Contact.Device is PointerDeviceKind.Mouse or PointerDeviceKind.Unknown)
                return default;

            var velocity = MaskAxes(_velocity.GetVelocity(e.Timestamp), options.Axes);
            var speed = Math.Sqrt(velocity.X * velocity.X + velocity.Y * velocity.Y);
            if (speed < options.MinFlickVelocity || speed == 0)
                return default;
            if (speed > options.MaxFlickVelocity)
                velocity *= options.MaxFlickVelocity / speed;
            return velocity;
        }
    }
}

/// <summary>
/// Estimates contact velocity from a ring buffer of the most recent position samples.
/// </summary>
internal sealed class VelocityTracker
{
    // Only samples this recent (milliseconds, relative to the newest sample) contribute.
    const double WINDOW_MS = 100;

    // A release this long after the last movement means the contact was stationary: no velocity.
    const double STALE_MS = 50;

    const int CAPACITY = 32;

    readonly double[] _times = new double[CAPACITY];
    readonly Point[] _positions = new Point[CAPACITY];
    int _next;
    int _count;

    /// <summary>Adds a position sample, overwriting the oldest when the buffer is full.</summary>
    public void Add(double timestamp, Point position)
    {
        _times[_next] = timestamp;
        _positions[_next] = position;
        _next = (_next + 1) % CAPACITY;
        if (_count < CAPACITY)
            _count++;
    }

    /// <summary>
    /// Returns the velocity in device pixels per second at the release time, using a least-squares line
    /// through the samples in the last <see cref="WINDOW_MS"/> milliseconds.  Returns zero if there are too few
    /// samples or the contact was stationary before release.
    /// </summary>
    public Point GetVelocity(double releaseTimestamp)
    {
        if (_count < 2)
            return default;

        var newest = (_next - 1 + CAPACITY) % CAPACITY;
        var newestTime = _times[newest];
        if (releaseTimestamp - newestTime > STALE_MS)
            return default;

        // Collect the recent samples, newest first.
        var n = 0;
        double sumT = 0, sumX = 0, sumY = 0;
        for (var i = 0; i < _count; i++)
        {
            var index = (newest - i + CAPACITY) % CAPACITY;
            if (newestTime - _times[index] > WINDOW_MS)
                break;
            n++;
            sumT += _times[index];
            sumX += _positions[index].X;
            sumY += _positions[index].Y;
        }

        if (n < 2)
            return default;

        var meanT = sumT / n;
        var meanX = sumX / n;
        var meanY = sumY / n;
        double covX = 0, covY = 0, varT = 0;
        for (var i = 0; i < n; i++)
        {
            var index = (newest - i + CAPACITY) % CAPACITY;
            var dt = _times[index] - meanT;
            covX += dt * (_positions[index].X - meanX);
            covY += dt * (_positions[index].Y - meanY);
            varT += dt * dt;
        }

        if (varT <= 0)
            return default;

        // Positions per millisecond, converted to per second.
        return new Point(covX / varT * 1000, covY / varT * 1000);
    }
}
