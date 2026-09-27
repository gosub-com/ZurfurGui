using ZurfurGui.Base;
using ZurfurGui.Layout;
using ZurfurGui.Property;
using ZurfurGui.Render;

namespace ZurfurGui.Controls;

public sealed partial class ScrollViewer
{
    Point _scrollOffset;
    bool _syncingScrollBars;

    public ScrollViewer()
    {
        InitializeControl();
        _contentViewport.View.Layout = new ContentViewportLayout(_contentWindow.View);
        View.Layout = new ScrollViewerLayout(this);
        horizontalScrollBar.DataContext.PropertyChanged += OnScrollBarDataChanged;
        verticalScrollBar.DataContext.PropertyChanged += OnScrollBarDataChanged;
    }

    /// <summary>
    /// Loads content in two phases because the current loader uses LoadContent both for a control's own ZUI content
    /// and for content supplied by its parent. The first call builds the ScrollViewer's internal children directly.
    /// A later call routes parent-supplied content into the existing content window, keeping it separate from the
    /// overlay scrollbars.
    ///
    /// Future loader improvements could provide separate template-content and parent-content lifecycle hooks. That
    /// would remove the need to infer the phase from the current child count, but is outside the initial ScrollViewer
    /// implementation.
    /// </summary>
    public void LoadContent(Properties[]? contents, Loader.ControlCreationContext context)
    {
        if (contents == null)
            return;

        // The first LoadContent call loads the ScrollViewer's own ZUI children. View has no children yet,
        // so these internal controls must be added directly to the ScrollViewer.
        if (View.Children.Count == 0)
        {
            foreach (var property in contents)
                View.AddChild(Loader.CreateControl(property, context).View);
            return;
        }

        // Later LoadContent calls contain content supplied by the parent. Route it into the already-created
        // content window so user content stays separate from the ScrollViewer's overlay controls.
        var contentWindow = View.FindByName("_contentWindow");
        foreach (var property in contents)
            contentWindow.AddChild(Loader.CreateControl(property, context).View);

        ScrollOffset = _scrollOffset;
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
            _contentWindow.View.SetProperty(Panel.Offset, new PointProp(-clamped.X, -clamped.Y));

            if (!_syncingScrollBars)
            {
                _syncingScrollBars = true;
                try
                {
                    horizontalData.Value = clamped.X;
                    verticalData.Value = clamped.Y;
                }
                finally
                {
                    _syncingScrollBars = false;
                }
            }
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

        SyncScrollBars();
    }

    void SyncScrollBars()
    {
        var viewportSize = _contentViewport.View.ContentRect.Size;
        var contentSize = _contentWindow.View.DesiredContentSize;

        _syncingScrollBars = true;
        try
        {
            SyncScrollBar(horizontalScrollBar, contentSize.Width, viewportSize.Width);
            SyncScrollBar(verticalScrollBar, contentSize.Height, viewportSize.Height);
            ScrollOffset = _scrollOffset;
        }
        finally
        {
            _syncingScrollBars = false;
        }
    }

    static void SyncScrollBar(ScrollBar scrollBar, double contentExtent, double viewportSize)
    {
        var data = scrollBar.DataContext;
        var maximum = Math.Max(0, contentExtent - viewportSize);
        data.Minimum = 0;
        data.Maximum = maximum;
        data.ViewportSize = Math.Max(0, viewportSize);
        data.LargeChange = Math.Max(0, viewportSize);

        scrollBar.View.IsVisible = data.Visibility switch
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
            var width = viewport.Size.Width - (verticalVisible ? verticalThickness : 0);
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
            var height = viewport.Size.Height - (horizontalVisible ? horizontalThickness : 0);
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
            _owner._contentViewport.View.Arrange(contentRect, measure);
            _owner.SyncScrollBars();
            _owner.ArrangeScrollBars(contentRect, measure);
        }
    }

    /// <summary>
    /// Measures user content without constraining it to the viewport and arranges it at its natural size inside
    /// the clipped viewport. It is separate from ScrollViewerLayout because the outer layout controls the
    /// viewport and overlay scrollbar rectangles, while this layout controls only the scrollable content window.
    /// </summary>
    sealed class ContentViewportLayout : Layoutable
    {
        readonly View _contentWindow;

        public ContentViewportLayout(View contentWindow)
        {
            _contentWindow = contentWindow;
        }

        public string TypeName => "ScrollViewerContentViewport";

        public Size MeasureView(View view, MeasureContext measure, Size available)
        {
            _contentWindow.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity), measure);
            var contentSize = _contentWindow.DesiredTotalSize;
            return new Size(
                Math.Min(contentSize.Width, available.Width),
                Math.Min(contentSize.Height, available.Height));
        }

        public void ArrangeViews(View view, MeasureContext measure)
        {
            var contentRect = view.ContentRect;
            _contentWindow.Arrange(new Rect(contentRect.Position, _contentWindow.DesiredTotalSize), measure);
        }
    }
}
