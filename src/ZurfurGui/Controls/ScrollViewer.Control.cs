using System.Diagnostics;
using ZurfurGui.Base;
using ZurfurGui.Input;
using ZurfurGui.Layout;
using ZurfurGui.Property;
using ZurfurGui.Render;
using ZurfurGui.Windows;

namespace ZurfurGui.Controls;

public enum ScrollMode
{
    Both,
    Horizontal,
    Vertical,
    None
}

public sealed partial class ScrollViewer
{
    // Flick deceleration: the velocity decays exponentially at this rate (per second).
    const double FLICK_FRICTION = 3.0;

    // A flick stops when its speed (view units per second) falls below this.
    const double FLICK_STOP_SPEED = 10;

    // A long frame is treated as this long (seconds) so a stalled frame does not make the content jump.
    const double FLICK_MAX_FRAME_TIME = 0.05;

    Point _scrollOffset;
    bool _syncingScrollBars;
    readonly PanRecognizer _panRecognizer;
    readonly Stopwatch _flickClock = new();
    AppWindow? _flickAppWindow;
    TimeSpan _lastFlickTime;
    Point _flickVelocity;

    public ScrollViewer()
    {
        InitializeControl();
        horizontalScrollBar.View.SetProperty(ScrollBar.ExpandOnHoverProperty, true);
        verticalScrollBar.View.SetProperty(ScrollBar.ExpandOnHoverProperty, true);
        horizontalScrollBar.View.AddEvent(Panel.PointerEntered, OnScrollBarPointerEntered);
        verticalScrollBar.View.AddEvent(Panel.PointerEntered, OnScrollBarPointerEntered);
        _contentViewport.View.Layout = new ContentViewportLayout(this);
        View.Layout = new ScrollViewerLayout(this);
        View.AddEvent(Panel.PointerWheel, OnPointerWheel);
        DataContext.PropertyChanged += OnScrollViewerDataChanged;
        horizontalScrollBar.DataContext.PropertyChanged += OnScrollBarDataChanged;
        verticalScrollBar.DataContext.PropertyChanged += OnScrollBarDataChanged;

        _panRecognizer = View.AddGestureRecognizer(new PanRecognizer());
        _panRecognizer.CatchOnDown = () => _flickAppWindow != null;
        _panRecognizer.PanStarted += OnPanStarted;
        _panRecognizer.PanMoved += OnPanMoved;
        _panRecognizer.PanEnded += OnPanEnded;
        UpdatePanAxes();
    }

    /// <summary>
    /// Options for panning with touch, pen, or mouse.  By default touch and pen pan and flick, and the
    /// mouse pans only with the right button.  Set <see cref="PanOptions.Slop"/> to zero (or one) to pan
    /// without a start threshold, set <see cref="PanOptions.MouseButtons"/> to <see cref="PointerButtons.Left"/>
    /// to pan with the left button, or turn off <see cref="PanOptions.Flick"/> to stop the content on release.
    /// The viewer controls <see cref="PanOptions.Axes"/> itself, from its scroll mode.
    /// </summary>
    public PanOptions PanOptions => _panRecognizer.Options;

    /// <summary>
    /// Receives parent-supplied content in the content window rather than beside the overlay scrollbars.
    /// The loader adds parent-supplied content to ContentHost, so this control only needs to provide the host.
    /// </summary>
    public View ContentHost => _contentWindow.View;

    bool CanScrollHorizontally => DataContext.ScrollMode is ScrollMode.Both or ScrollMode.Horizontal;
    bool CanScrollVertically => DataContext.ScrollMode is ScrollMode.Both or ScrollMode.Vertical;

    /// <summary>
    /// Raises the scrollbar that received the pointer above its sibling views.
    /// </summary>
    void OnScrollBarPointerEntered(object? sender, PointerEvent e)
    {
        if (sender is View view)
            view.BringToFront();
    }

    public Point ScrollOffset
    {
        get => _scrollOffset;
        set
        {
            var horizontalData = horizontalScrollBar.DataContext;
            var verticalData = verticalScrollBar.DataContext;
            var clamped = new Point(
                Math.Clamp(value.X, horizontalData.Minimum, horizontalData.Maximum),
                Math.Clamp(value.Y, verticalData.Minimum, verticalData.Maximum));

            _scrollOffset = clamped;
            _contentWindow.View.SetProperty(Panel.VisualOffset, new PointProp(-clamped.X, -clamped.Y));

            var wasSyncingScrollBars = _syncingScrollBars;
            _syncingScrollBars = true;
            try
            {
                horizontalData.Value = clamped.X;
                verticalData.Value = clamped.Y;
            }
            finally
            {
                _syncingScrollBars = wasSyncingScrollBars;
            }
        }
    }

    /// <summary>
    /// Scrolls the content by WheelSteps scroll steps per wheel detent. Shift turns a vertical wheel into a
    /// horizontal one, and Ctrl is ignored. The wheel is marked handled when the viewer can scroll in the
    /// direction of the wheel, even when already at the limit, so it does not reach a parent scroller. A
    /// viewer that cannot scroll in that direction leaves the event unhandled so a parent can scroll.
    /// </summary>
    void OnPointerWheel(object? sender, WheelEvent e)
    {
        var modifiers = e.Contact.Modifiers;
        if ((modifiers & PointerModifiers.Control) != 0)
        {
            e.Handled = true;
            return;
        }

        var notchesX = e.NotchesX;
        var notchesY = e.NotchesY;
        if ((modifiers & PointerModifiers.Shift) != 0 && notchesX == 0)
        {
            notchesX = notchesY;
            notchesY = 0;
        }

        if (!(CanScrollHorizontally && notchesX != 0) && !(CanScrollVertically && notchesY != 0))
            return;

        e.Handled = true;
        StopFlick();
        var step = DataContext.ScrollSize.Or(16);
        var distanceX = CanScrollHorizontally ? notchesX * Properties.WheelSteps * step.Width : 0;
        var distanceY = CanScrollVertically ? notchesY * Properties.WheelSteps * step.Height : 0;
        ScrollOffset = new Point(ScrollOffset.X + distanceX, ScrollOffset.Y + distanceY);
    }

    // Stops a pan from a previous contact and starts moving the content with the new one.  The slop
    // displacement is ignored so the content does not jump when the pan starts.
    void OnPanStarted(object? sender, PanEventArgs e)
    {
        StopFlick();
    }

    // Moves the content with the contact.  Content follows the finger, so the offset moves the opposite way.
    void OnPanMoved(object? sender, PanEventArgs e)
    {
        var delta = e.Delta / View.Scale;
        ScrollOffset = new Point(ScrollOffset.X - delta.X, ScrollOffset.Y - delta.Y);
    }

    // Starts a flick if the contact was released with velocity.
    void OnPanEnded(object? sender, PanEventArgs e)
    {
        if (e.Cancelled || (e.Velocity.X == 0 && e.Velocity.Y == 0) || View.AppWindow is not AppWindow appWindow)
            return;

        var velocity = e.Velocity / View.Scale;
        _flickVelocity = new Point(-velocity.X, -velocity.Y);
        _lastFlickTime = TimeSpan.Zero;
        _flickClock.Restart();
        _flickAppWindow = appWindow;
        appWindow.PreRenderFrame += OnFlickFrame;
    }

    // Continues the pan after release.  The recognizer knows nothing about the scroll range, so the flick
    // on an axis stops here when the offset reaches its limit.
    void OnFlickFrame(object? sender, EventArgs e)
    {
        var now = _flickClock.Elapsed;
        var dt = Math.Min((now - _lastFlickTime).TotalSeconds, FLICK_MAX_FRAME_TIME);
        _lastFlickTime = now;
        if (dt <= 0)
            return;

        var decay = Math.Exp(-FLICK_FRICTION * dt);
        var distanceFactor = (1 - decay) / FLICK_FRICTION;
        var target = new Point(
            _scrollOffset.X + _flickVelocity.X * distanceFactor,
            _scrollOffset.Y + _flickVelocity.Y * distanceFactor);
        ScrollOffset = target;

        var velocityX = ScrollOffset.X != target.X ? 0 : _flickVelocity.X * decay;
        var velocityY = ScrollOffset.Y != target.Y ? 0 : _flickVelocity.Y * decay;
        _flickVelocity = new Point(velocityX, velocityY);
        if (Math.Sqrt(velocityX * velocityX + velocityY * velocityY) < FLICK_STOP_SPEED)
            StopFlick();
    }

    // Stops the flick, if there is one.
    void StopFlick()
    {
        if (_flickAppWindow is AppWindow appWindow)
            appWindow.PreRenderFrame -= OnFlickFrame;

        _flickAppWindow = null;
        _flickVelocity = default;
    }

    // The viewer pans only in the directions it can scroll, so a viewer that cannot scroll in a direction
    // lets an ancestor pan.  Being at the scroll limit does not change this.
    void UpdatePanAxes()
    {
        var axes = PanAxes.None;
        if (CanScrollHorizontally)
            axes |= PanAxes.Horizontal;
        if (CanScrollVertically)
            axes |= PanAxes.Vertical;
        _panRecognizer.Options.Axes = axes;
    }

    void OnScrollViewerDataChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ScrollViewerData.ScrollMode))
        {
            UpdatePanAxes();
            _contentViewport.View.InvalidateMeasure();
            View.InvalidateMeasure();
            SyncScrollBars(_contentViewport.View.ContentRect.Size);
            return;
        }

        if (e.PropertyName != nameof(ScrollViewerData.ScrollSize))
            return;

        var scrollSize = DataContext.ScrollSize.Or(16);
        var wasSyncingScrollBars = _syncingScrollBars;
        _syncingScrollBars = true;
        try
        {
            horizontalScrollBar.DataContext.SmallChange = scrollSize.Width;
            verticalScrollBar.DataContext.SmallChange = scrollSize.Height;
        }
        finally
        {
            _syncingScrollBars = wasSyncingScrollBars;
        }
    }



    void OnScrollBarDataChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_syncingScrollBars)
            return;

        if (e.PropertyName == nameof(ScrollBarData.Value))
        {
            StopFlick();
            ScrollOffset = new Point(
                horizontalScrollBar.DataContext.Value,
                verticalScrollBar.DataContext.Value);
            return;
        }

        SyncScrollBars(_contentViewport.View.ContentRect.Size);
    }

    void SyncScrollBars(Size viewportSize)
    {
        UpdatePanAxes();
        var contentSize = _contentWindow.View.DesiredContentSize;
        var verticalData = verticalScrollBar.DataContext;
        var wasAtVerticalEnd = verticalData.Value >= verticalData.Maximum;

        _syncingScrollBars = true;
        try
        {
            var scrollSize = DataContext.ScrollSize.Or(16);
            SyncScrollBar(horizontalScrollBar, contentSize.Width, viewportSize.Width, scrollSize.Width, CanScrollHorizontally);
            SyncScrollBar(verticalScrollBar, contentSize.Height, viewportSize.Height, scrollSize.Height, CanScrollVertically);
            ScrollOffset = _scrollOffset;

            if (Properties.AutoScrollToEnd && wasAtVerticalEnd)
                ScrollOffset = new Point(ScrollOffset.X, verticalData.Maximum);
        }
        finally
        {
            _syncingScrollBars = false;
        }
    }

    static void SyncScrollBar(
        ScrollBar scrollBar,
        double contentExtent,
        double viewportSize,
        double scrollSize,
        bool canScroll)
    {
        var data = scrollBar.DataContext;
        var maximum = canScroll ? Math.Max(0, contentExtent - viewportSize) : 0;
        data.Minimum = 0;
        data.Maximum = maximum;
        data.ViewportSize = Math.Max(0, viewportSize);
        data.SmallChange = scrollSize;
        data.LargeChange = Math.Max(0, viewportSize);

        scrollBar.View.IsVisible = !canScroll ? false : data.Visibility switch
        {
            ScrollBarVisibility.Visible => true,
            ScrollBarVisibility.Hidden => false,
            _ => maximum > 0
        };
    }

    void ArrangeScrollBars(Rect viewport, MeasureContext measure)
    {
        var horizontalVisible = horizontalScrollBar.View.IsVisible;
        var verticalVisible = verticalScrollBar.View.IsVisible;
        var horizontalThickness = horizontalScrollBar.View.DesiredTotalSize.Height;
        var verticalThickness = verticalScrollBar.View.DesiredTotalSize.Width;

        if (horizontalVisible)
        {
            var width = viewport.Size.Width;
            horizontalScrollBar.View.Arrange(
                new Rect(
                    viewport.X,
                    viewport.Bottom - horizontalThickness,
                    Math.Max(0, width),
                    horizontalThickness),
                measure);
        }

        if (verticalVisible)
        {
            var height = viewport.Size.Height;
            verticalScrollBar.View.Arrange(
                new Rect(
                    viewport.Right - verticalThickness,
                    viewport.Y,
                    verticalThickness,
                    Math.Max(0, height)),
                measure);
        }
    }


    /// <summary>
    /// Measures the ScrollViewer's viewport and scrollbar children, then arranges them as overlay controls.
    /// This layout is separate from ContentViewportLayout because the ScrollViewer must keep its viewport at
    /// the full available size while positioning the scrollbars around the empty bottom-right corner.
    /// </summary>
    sealed class ScrollViewerLayout : Layoutable
    {
        readonly ScrollViewer _owner;

        public ScrollViewerLayout(ScrollViewer owner)
        {
            _owner = owner;
        }

        public string TypeName => "ScrollViewer";

        public Size MeasureView(View view, MeasureContext measure, Size available)
        {
            foreach (var child in view.Children)
            {
                // Scrollbar visibility is resolved after the content has been measured. Measure both bars
                // independently so their cross-axis thickness is available even when Auto later hides one.
                if (child == _owner.horizontalScrollBar.View || child == _owner.verticalScrollBar.View)
                    child.IsVisible = true;

                child.Measure(available, measure);
            }

            // Overlay scrollbars do not contribute to the desired size. The viewport reports the smaller of
            // the content's natural size and the available size, so a viewer with small content can wrap while
            // larger content still uses the finite available area as its viewport.
            return _owner._contentViewport.View.DesiredTotalSize;
        }

        public void ArrangeViews(View view, MeasureContext measure)
        {
            var contentRect = view.ContentRect;
            _owner.SyncScrollBars(contentRect.Size);
            _owner._contentViewport.View.Arrange(contentRect, measure);
            _owner.ArrangeScrollBars(contentRect, measure);
        }
    }

    /// <summary>
    /// Measures user content with constraints determined by ScrollMode and arranges it inside the clipped viewport.
    /// It is separate from ScrollViewerLayout because the outer layout controls the viewport and overlay scrollbar
    /// rectangles, while this layout controls only the scrollable content window.
    /// </summary>
    sealed class ContentViewportLayout : Layoutable
    {
        readonly ScrollViewer _owner;

        public ContentViewportLayout(ScrollViewer owner)
        {
            _owner = owner;
        }

        public string TypeName => "ScrollViewerContentViewport";

        public Size MeasureView(View view, MeasureContext measure, Size available)
        {
            var canScrollHorizontally = _owner.CanScrollHorizontally;
            var canScrollVertically = _owner.CanScrollVertically;
            var contentAvailable = new Size(
                canScrollHorizontally ? double.PositiveInfinity : available.Width,
                canScrollVertically ? double.PositiveInfinity : available.Height);
            _owner._contentWindow.View.Measure(contentAvailable, measure);
            var contentSize = _owner._contentWindow.View.DesiredTotalSize;
            return new Size(
                canScrollHorizontally ? Math.Min(contentSize.Width, available.Width) : contentSize.Width,
                canScrollVertically ? Math.Min(contentSize.Height, available.Height) : contentSize.Height);
        }

        public void ArrangeViews(View view, MeasureContext measure)
        {
            var contentRect = view.ContentRect;
            var contentSize = _owner._contentWindow.View.DesiredTotalSize;
            var arrangeSize = new Size(
                _owner.CanScrollHorizontally ? contentSize.Width : contentRect.Width,
                _owner.CanScrollVertically ? contentSize.Height : contentRect.Height);
            _owner._contentWindow.View.Arrange(
                new Rect(contentRect.Position, arrangeSize),
                measure);
        }
    }
}
