using ZurfurGui.Base;
using ZurfurGui.Input;
using ZurfurGui.Layout;
using ZurfurGui.Property;
using ZurfurGui.Render;

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
    Point _scrollOffset;
    bool _syncingScrollBars;

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
    }

    /// <summary>
    /// Receives parent-supplied content in the content window rather than beside the overlay scrollbars.
    /// The default LoadParentContent implementation uses ContentHost, so this control only needs to provide the host.
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
    /// horizontal one, and Ctrl is ignored. The router gives this view the wheel even when it cannot scroll further.
    /// </summary>
    void OnPointerWheel(object? sender, WheelEvent e)
    {
        var modifiers = e.Contact.Modifiers;
        if ((modifiers & PointerModifiers.Control) != 0)
            return;

        var notchesX = e.NotchesX;
        var notchesY = e.NotchesY;
        if ((modifiers & PointerModifiers.Shift) != 0 && notchesX == 0)
        {
            notchesX = notchesY;
            notchesY = 0;
        }

        var step = DataContext.ScrollSize.Or(16);
        var distanceX = CanScrollHorizontally ? notchesX * Properties.WheelSteps * step.Width : 0;
        var distanceY = CanScrollVertically ? notchesY * Properties.WheelSteps * step.Height : 0;
        ScrollOffset = new Point(ScrollOffset.X + distanceX, ScrollOffset.Y + distanceY);
    }

    void OnScrollViewerDataChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ScrollViewerData.ScrollMode))
        {
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
            ScrollOffset = new Point(
                horizontalScrollBar.DataContext.Value,
                verticalScrollBar.DataContext.Value);
            return;
        }

        SyncScrollBars(_contentViewport.View.ContentRect.Size);
    }

    void SyncScrollBars(Size viewportSize)
    {
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
