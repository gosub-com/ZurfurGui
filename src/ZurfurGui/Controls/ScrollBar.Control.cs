using ZurfurGui.Base;
using ZurfurGui.Layout;
using ZurfurGui.Platform;
using ZurfurGui.Property;
using ZurfurGui.Render;

namespace ZurfurGui.Controls;

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
        View.Draw = new DrawScrollBar(this);

        // Subscribe to pointer events
        View.AddEvent(Panel.PointerDown, OnPointerDown);
        View.AddEvent(Panel.PointerMove, OnPointerMove);
        View.AddEvent(Panel.PointerUp, OnPointerUp);
        View.AddEvent(Panel.PointerCaptureLost, OnPointerCaptureLost);
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
        var trackSize = orientationValue == Orientation.Vertical ? View.Size.Height : View.Size.Width;
        var thumbSize = CalculateThumbSize();
        var availableTrack = trackSize - thumbSize;

        var ratio = (value - data.Minimum) / (data.Maximum - data.Minimum);
        return ratio * availableTrack;
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
        var trackSize = orientationValue == Orientation.Vertical ? View.Size.Height : View.Size.Width;
        var thumbSize = CalculateThumbSize();
        var availableTrack = trackSize - thumbSize;

        if (availableTrack <= 0)
            return data.Minimum;

        var ratio = screenPos / availableTrack;
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
        var trackSize = orientationValue == Orientation.Vertical ? View.Size.Height : View.Size.Width;

        var thumbRatio = data.ViewportSize / range;
        var thumbSize = Math.Max(thumbMinSize, thumbRatio * trackSize);
        return Math.Min(thumbSize, trackSize);
    }

    void OnPointerDown(object? sender, PointerEvent e)
    {
        // Convert from device coordinates to local control coordinates
        var viewPos = View.toClient(e.DevicePosition);
        var thumbRect = CalculateThumbRect();

        if (thumbRect.Contains(viewPos))
        {
            // Start thumb drag
            _isDragging = true;
            _dragStartValue = DataContext.Value;
            _dragStartPoint = viewPos;
            View.SetProperty(Panel.IsPressed, true);
            View.CapturePointer = true;
        }
        else
        {
            // Clicked on track - move thumb to click position
            var data = DataContext;
            var orientationValue = View.GetStyle(OrientationProperty);

            // Get the click position along the track axis
            var clickPos = orientationValue == Orientation.Vertical
                ? viewPos.Y
                : viewPos.X;

            // Move up or down
            if (ScreenToValue(clickPos) < data.Value)
                data.Value = Math.Max(data.Minimum, data.Value - data.LargeChange);
            else
                data.Value = Math.Min(data.Maximum, data.Value + data.LargeChange);

        }
    }

    void OnPointerMove(object? sender, PointerEvent e)
    {
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
        var trackSize = orientationValue == Orientation.Vertical ? View.Size.Height : View.Size.Width;
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
            View.SetProperty(Panel.IsPressed, false);
            View.CapturePointer = false;
        }
    }

    void OnPointerCaptureLost(object? sender, EventArgs e)
    {
        _isDragging = false;
        View.SetProperty(Panel.IsPressed, false);
    }
}

