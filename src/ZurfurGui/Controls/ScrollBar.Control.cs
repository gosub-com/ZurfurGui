using ZurfurGui.Base;
using ZurfurGui.Input;
using ZurfurGui.Layout;
using ZurfurGui.Property;
using ZurfurGui.Render;

namespace ZurfurGui.Controls;

public enum ScrollBarHitRegion
{
    None,
    StartArrow,
    EndArrow,
    Track,
    Thumb
}

public enum ScrollBarVisibility
{
    Auto,
    Visible,
    Hidden
}

public partial class ScrollBar : Controllable, Renderable
{
    // Drag state (not in data - pure UI state)
    bool _isDragging;
    bool _isPointerDown;
    double _dragStartValue;
    Point _dragStartPoint;

    internal bool IsDragging => _isDragging;
    internal bool IsPointerDown => _isPointerDown;
    internal bool IsExpanded => Properties.IsExpanded;


    static ScrollBar()
    {
        // The generated PropertyKeys need their flags set if they affect layout or rendering
        // (This static constructor runs before any instances are created)
    }

    public ScrollBar()
    {
        InitializeControl();
        View.Render = this;

        // Subscribe to pointer events
        View.AddEvent(Panel.PointerDown, OnPointerDown);
        View.AddEvent(Panel.PointerMove, OnPointerMove);
        View.AddEvent(Panel.PointerUp, OnPointerUp);
        View.AddEvent(Panel.PointerCaptureLost, OnPointerCaptureLost);
        View.PropertyChanged += OnViewPropertyChanged;
        UpdateExpandedState();
    }

    void OnViewPropertyChanged(IPropertyKey property)
    {
        if (property.Id == Panel.IsPointerOver.Id
            || property.Id == Panel.IsPressed.Id
            || property.Id == ExpandOnHoverProperty.Id)
        {
            UpdateExpandedState();
        }
    }

    internal void UpdateExpandedState()
    {
        var isPressed = View.GetProperty(Panel.IsPressed) || IsPointerDown || IsDragging;
        var appWindow = View.AppWindow;
        var hasActivePointerInput = appWindow is not null
            && (appWindow.PointerContacts.Any(contact => contact.Buttons != PointerButtons.None)
                || appWindow.CapturedPointerContacts.Count != 0);
        var isExpanded = !Properties.ExpandOnHover
            || (View.GetProperty(Panel.IsPointerOver) && !hasActivePointerInput)
            || isPressed;

        if (IsExpanded != isExpanded)
        {
            var properties = Properties;
            properties.IsExpanded = isExpanded;
            View.InvalidateStyleCacheInternal();
        }
    }

    /// <summary>
    /// Get the size of one arrow button (square: equals scrollbar width/height).
    /// </summary>
    private double GetArrowSize(Orientation orientation)
    {
        return orientation == Orientation.Vertical ? View.Size.Width : View.Size.Height;
    }

    /// <summary>
    /// Get the rectangle for the start arrow (top for vertical, left for horizontal).
    /// </summary>
    public Rect GetStartArrowRect()
    {
        var orientationValue = Properties.Orientation;
        var arrowSize = GetArrowSize(orientationValue);

        if (orientationValue == Orientation.Vertical)
            return new Rect(0, 0, View.Size.Width, arrowSize);
        else
            return new Rect(0, 0, arrowSize, View.Size.Height);
    }

    /// <summary>
    /// Get the rectangle for the end arrow (bottom for vertical, right for horizontal).
    /// </summary>
    public Rect GetEndArrowRect()
    {
        var orientationValue = Properties.Orientation;
        var arrowSize = GetArrowSize(orientationValue);

        if (orientationValue == Orientation.Vertical)
            return new Rect(0, View.Size.Height - arrowSize, View.Size.Width, arrowSize);
        else
            return new Rect(View.Size.Width - arrowSize, 0, arrowSize, View.Size.Height);
    }

    /// <summary>
    /// Convert a value to screen position along the track (in local coordinates).
    /// </summary>
    public double ValueToScreen(double value)
    {
        var data = DataContext;
        if (data.Maximum <= data.Minimum)
            return 0;

        var orientationValue = Properties.Orientation;
        var arrowSize = GetArrowSize(orientationValue);
        var fullSize = orientationValue == Orientation.Vertical ? View.Size.Height : View.Size.Width;
        var trackSize = fullSize - (2 * arrowSize);  // Subtract both arrows
        var thumbSize = CalculateThumbSize();
        var availableTrack = trackSize - thumbSize;

        var ratio = (value - data.Minimum) / (data.Maximum - data.Minimum);
        return arrowSize + (ratio * availableTrack);  // Offset by start arrow
    }

    /// <summary>
    /// Convert a screen position to a value (inverse of ValueToScreen).
    /// </summary>
    public double ScreenToValue(double screenPos)
    {
        var data = DataContext;
        if (data.Maximum <= data.Minimum)
            return data.Minimum;

        var orientationValue = Properties.Orientation;
        var arrowSize = GetArrowSize(orientationValue);
        var fullSize = orientationValue == Orientation.Vertical ? View.Size.Height : View.Size.Width;
        var trackSize = fullSize - (2 * arrowSize);  // Subtract both arrows
        var thumbSize = CalculateThumbSize();
        var availableTrack = trackSize - thumbSize;

        if (availableTrack <= 0)
            return data.Minimum;

        var adjustedPos = screenPos - arrowSize;  // Adjust for start arrow
        var ratio = adjustedPos / availableTrack;
        return data.Minimum + ratio * (data.Maximum - data.Minimum);
    }

    /// <summary>
    /// Calculate the thumb rectangle in local coordinates.
    /// </summary>
    public Rect CalculateThumbRect()
    {
        var data = DataContext;
        var orientationValue = Properties.Orientation;
        var thumbSize = CalculateThumbSize();
        var thumbPos = ValueToScreen(data.Value);

        if (orientationValue == Orientation.Vertical)
        {
            return new Rect(0, thumbPos, View.Size.Width, thumbSize);
        }
        else
        {
            return new Rect(thumbPos, 0, thumbSize, View.Size.Height);
        }
    }

    /// <summary>
    /// Calculate the visual thumb inside the scrollbar's interaction rectangle.
    /// The resting thumb is thin and near the outer edge; the active thumb is wider and centered.
    /// The interaction rectangle and thumb position/length do not change between visual states.
    /// </summary>
    public Rect CalculateVisualThumbRect(bool isExpanded)
    {
        var thumbRect = CalculateThumbRect();
        var thickness = Math.Max(0, isExpanded ? Properties.ThumbThickness : Properties.ThumbRestingThickness);
        var edgeInset = Math.Max(0, Properties.ThumbEdgeInset);

        if (Properties.Orientation == Orientation.Vertical)
        {
            thickness = Math.Min(thickness, View.Size.Width);
            var x = isExpanded
                ? (View.Size.Width - thickness) / 2
                : Math.Max(0, View.Size.Width - thickness - edgeInset);
            return new Rect(
                x,
                thumbRect.Y,
                thickness,
                thumbRect.Height);
        }

        thickness = Math.Min(thickness, View.Size.Height);
        var y = isExpanded
            ? (View.Size.Height - thickness) / 2
            : Math.Max(0, View.Size.Height - thickness - edgeInset);
        return new Rect(
            thumbRect.X,
            y,
            thumbRect.Width,
            thickness);
    }

    /// <summary>
    /// Calculate the thumb size based on viewport ratio.
    /// </summary>
    double CalculateThumbSize()
    {
        var data = DataContext;
        var range = data.Maximum - data.Minimum + data.ViewportSize;
        if (range <= 0)
            return 0;

        var thumbMinSize = Properties.ThumbMinSize;
        var orientationValue = Properties.Orientation;
        var arrowSize = GetArrowSize(orientationValue);
        var fullSize = orientationValue == Orientation.Vertical ? View.Size.Height : View.Size.Width;
        var trackSize = fullSize - (2 * arrowSize);  // Subtract both arrows

        var thumbRatio = data.ViewportSize / range;
        var thumbSize = Math.Max(thumbMinSize, thumbRatio * trackSize);
        return Math.Min(thumbSize, trackSize);
    }

    /// <summary>
    /// Determine which region of the scrollbar contains the given point.
    /// </summary>
    private ScrollBarHitRegion HitTest(Point localPoint)
    {
        if (GetStartArrowRect().Contains(localPoint))
            return ScrollBarHitRegion.StartArrow;

        if (GetEndArrowRect().Contains(localPoint))
            return ScrollBarHitRegion.EndArrow;

        if (CalculateThumbRect().Contains(localPoint))
            return ScrollBarHitRegion.Thumb;

        return ScrollBarHitRegion.Track;
    }

    void OnPointerDown(object? sender, PointerEvent e)
    {
        // Convert from device coordinates to local control coordinates
        var viewPos = View.toClient(e.Contact.DevicePosition);
        var region = HitTest(viewPos);
        var data = DataContext;

        // Capture every scrollbar interaction so pressed visuals and pointer events continue outside the
        // scrollbar's original hit area. PointerOver clears Panel.IsPressed when hover leaves, so maintain the
        // pressed state locally until pointer-up or capture-loss.
        _isPointerDown = true;
        View.SetProperty(Panel.IsPressed, true);
        View.SetCapture(e.Contact.Key, true);

        switch (region)
        {
            case ScrollBarHitRegion.StartArrow:
                // Decrease value by SmallChange
                data.Value = Math.Max(data.Minimum, data.Value - data.SmallChange);
                break;

            case ScrollBarHitRegion.EndArrow:
                // Increase value by SmallChange
                data.Value = Math.Min(data.Maximum, data.Value + data.SmallChange);
                break;

            case ScrollBarHitRegion.Thumb:
                // Start thumb drag
                _isDragging = true;
                _dragStartValue = data.Value;
                _dragStartPoint = viewPos;
                break;

            case ScrollBarHitRegion.Track:
                // Jump by LargeChange in the direction of click
                var orientationValue = Properties.Orientation;
                var clickPos = orientationValue == Orientation.Vertical ? viewPos.Y : viewPos.X;

                if (ScreenToValue(clickPos) < data.Value)
                    data.Value = Math.Max(data.Minimum, data.Value - data.LargeChange);
                else
                    data.Value = Math.Min(data.Maximum, data.Value + data.LargeChange);
                break;
        }
    }

    void OnPointerMove(object? sender, PointerEvent e)
    {
        UpdateExpandedState();

        if (_isPointerDown)
            View.SetProperty(Panel.IsPressed, true);

        // Handle dragging
        if (!_isDragging)
            return;

        // Convert from device coordinates to local control coordinates
        var viewPos = View.toClient(e.Contact.DevicePosition);

        var data = DataContext;
        var orientationValue = Properties.Orientation;
        var delta = orientationValue == Orientation.Vertical 
            ? viewPos.Y - _dragStartPoint.Y 
            : viewPos.X - _dragStartPoint.X;

        var thumbSize = CalculateThumbSize();
        var arrowSize = GetArrowSize(orientationValue);
        var fullSize = orientationValue == Orientation.Vertical ? View.Size.Height : View.Size.Width;
        var trackSize = fullSize - (2 * arrowSize);
        var availableTrack = trackSize - thumbSize;

        if (availableTrack > 0)
        {
            var valueDelta = delta / availableTrack * (data.Maximum - data.Minimum);
            data.Value = Math.Clamp(
                _dragStartValue + valueDelta,
                data.Minimum,
                data.Maximum
            );
        }
    }

    void OnPointerUp(object? sender, PointerEvent e)
    {
        _isDragging = false;
        _isPointerDown = false;
        View.SetCapture(e.Contact.Key, false);
    }

    void OnPointerCaptureLost(object? sender, PointerContactKey _)
    {
        _isDragging = false;
        _isPointerDown = false;
    }


    public RenderResult Render(View view, RenderContext context, Rect clientClip)
    {
        var data = DataContext;
        var trackRect = new Rect(new(), view.Size);
        var orientationValue = Properties.Orientation;
        var isExpanded = IsExpanded;

        if (isExpanded)
        {
            // Draw background track
            var trackColor = Properties.TrackColor;
            context.FillRect(trackColor, trackRect, 1000);
        }

        // The thumb keeps the same position and length along the scroll direction in every visual state. At rest
        // it is thin and near the outer edge; hover and press make it wider and centered.
        if (data.Maximum > data.Minimum)
        {
            var thumbRect = CalculateVisualThumbRect(isExpanded);
            var thumbColor = Properties.ThumbColor;
            var thumbRadius = Properties.ThumbRadius;
            context.FillRect(thumbColor, thumbRect, thumbRadius);

            if (isExpanded)
            {
                var startArrowRect = GetStartArrowRect();
                var startArrowPoints = GetArrowTrianglePoints(startArrowRect, orientationValue, true);
                context.FillPolygon(new Brush(thumbColor), startArrowPoints);

                var endArrowRect = GetEndArrowRect();
                var endArrowPoints = GetArrowTrianglePoints(endArrowRect, orientationValue, false);
                context.FillPolygon(new Brush(thumbColor), endArrowPoints);
            }
        }
        return RenderResult.RenderedAll;
    }

    /// <summary>
    /// Generate triangle points for an arrow within the given rectangle.
    /// Triangle is inset by 25% padding from edges.
    /// </summary>
    /// <param name="arrowRect">The rectangle containing the arrow</param>
    /// <param name="orientation">Scrollbar orientation</param>
    /// <param name="pointsToStart">True for start arrow (up/left), false for end arrow (down/right)</param>
    /// <returns>Array of triangle points as [x1, y1, x2, y2, x3, y3]</returns>
    double[] GetArrowTrianglePoints(Rect arrowRect, Orientation orientation, bool pointsToStart)
    {
        var padding = 0.25;  // 25% padding on each side
        var centerX = arrowRect.X + arrowRect.Width / 2;
        var centerY = arrowRect.Y + arrowRect.Height / 2;

        if (orientation == Orientation.Vertical)
        {
            // Vertical scrollbar: up or down arrow
            var leftX = arrowRect.X + arrowRect.Width * padding;
            var rightX = arrowRect.X + arrowRect.Width * (1 - padding);
            var tipY = pointsToStart
                ? arrowRect.Y + arrowRect.Height * padding          // Points up
                : arrowRect.Y + arrowRect.Height * (1 - padding);   // Points down
            var baseY = pointsToStart
                ? arrowRect.Y + arrowRect.Height * (1 - padding)    // Base at bottom
                : arrowRect.Y + arrowRect.Height * padding;         // Base at top

            return
            [
                centerX, tipY,      // Tip (top or bottom)
                rightX, baseY,      // Right base
                leftX, baseY        // Left base
            ];
        }
        else
        {
            // Horizontal scrollbar: left or right arrow
            var topY = arrowRect.Y + arrowRect.Height * padding;
            var bottomY = arrowRect.Y + arrowRect.Height * (1 - padding);
            var tipX = pointsToStart
                ? arrowRect.X + arrowRect.Width * padding           // Points left
                : arrowRect.X + arrowRect.Width * (1 - padding);    // Points right
            var baseX = pointsToStart
                ? arrowRect.X + arrowRect.Width * (1 - padding)     // Base at right
                : arrowRect.X + arrowRect.Width * padding;          // Base at left

            return
            [
                tipX, centerY,      // Tip (left or right)
                baseX, bottomY,     // Bottom base
                baseX, topY         // Top base
            ];
        }
    }

    public bool IsHit(View view, HitTestContext context)
    {
        var p = view.toClient(context.Contact.DevicePosition);
        return new Rect(new(), view.Size).Contains(p);
    }


}

