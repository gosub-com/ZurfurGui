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

public partial class ScrollBar : Controllable
{
    // Drag state (not in data - pure UI state)
    bool _isDragging;
    double _dragStartValue;
    Point _dragStartPoint;

    static ScrollBar()
    {
        // The generated PropertyKeys need their flags set if they affect layout or rendering
        // (This static constructor runs before any instances are created)
    }

    public ScrollBar()
    {
        InitializeControl();
        View.Render = new ScrollBarRenderer(this);

        // Subscribe to pointer events
        View.AddEvent(Panel.PointerDown, OnPointerDown);
        View.AddEvent(Panel.PointerMove, OnPointerMove);
        View.AddEvent(Panel.PointerUp, OnPointerUp);
        View.AddEvent(Panel.PointerCaptureLost, OnPointerCaptureLost);
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
        var orientationValue = View.GetStyle(OrientationProperty);
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
        var orientationValue = View.GetStyle(OrientationProperty);
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

        var orientationValue = View.GetStyle(OrientationProperty);
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

        var orientationValue = View.GetStyle(OrientationProperty);
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
        var orientationValue = View.GetStyle(ScrollBar.OrientationProperty);
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
    /// Calculate the thumb size based on viewport ratio.
    /// </summary>
    double CalculateThumbSize()
    {
        var data = DataContext;
        var range = data.Maximum - data.Minimum + data.ViewportSize;
        if (range <= 0)
            return 0;

        var thumbMinSize = View.GetStyle(ThumbMinSizeProperty);
        var orientationValue = View.GetStyle(OrientationProperty);
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
        var viewPos = View.toClient(e.DevicePosition);
        var region = HitTest(viewPos);
        var data = DataContext;

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
                View.CapturePointer = true;
                break;

            case ScrollBarHitRegion.Track:
                // Jump by LargeChange in the direction of click
                var orientationValue = View.GetStyle(OrientationProperty);
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
        // Handle dragging
        if (!_isDragging)
            return;

        // Convert from device coordinates to local control coordinates
        var viewPos = View.toClient(e.DevicePosition);

        var data = DataContext;
        var orientationValue = View.GetStyle(OrientationProperty);
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
        if (_isDragging)
        {
            _isDragging = false;
            View.CapturePointer = false;
        }
    }

    void OnPointerCaptureLost(object? sender, EventArgs e)
    {
        _isDragging = false;
    }
}

