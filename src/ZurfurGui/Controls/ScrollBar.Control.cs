using ZurfurGui.Base;
using ZurfurGui.Layout;
using ZurfurGui.Platform;
using ZurfurGui.Property;
using ZurfurGui.Render;

namespace ZurfurGui.Controls;

public partial class ScrollBar : Controllable
{
    // Appearance properties (styled)
    public static readonly PropertyKey<Color> ThumbColor = 
        new("ScrollBar.thumbColor", typeof(ScrollBar), new Color(128, 128, 128), ViewFlags.ReDraw);

    public static readonly PropertyKey<Color> TrackColor = 
        new("ScrollBar.trackColor", typeof(ScrollBar), new Color(220, 220, 220), ViewFlags.ReDraw);

    public static readonly PropertyKey<Color> ThumbHoverColor = 
        new("ScrollBar.thumbHoverColor", typeof(ScrollBar), new Color(102, 102, 102), ViewFlags.ReDraw);

    public static readonly PropertyKey<Color> ThumbPressedColor = 
        new("ScrollBar.thumbPressedColor", typeof(ScrollBar), new Color(77, 77, 77), ViewFlags.ReDraw);

    public static readonly PropertyKey<double> ThumbMinSize = 
        new("ScrollBar.thumbMinSize", typeof(ScrollBar), 20.0, ViewFlags.ReMeasure);

    public static readonly PropertyKey<Color> BorderColor = 
        new("ScrollBar.borderColor", typeof(ScrollBar), new Color(160, 160, 160), ViewFlags.ReDraw);

    public static readonly PropertyKey<double> BorderWidth = 
        new("ScrollBar.borderWidth", typeof(ScrollBar), 1.0, ViewFlags.ReDraw);

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

        var orientationValue = View.GetStyle(ScrollBar.OrientationProperty);
        var trackSize = orientationValue == Base.Orientation.Vertical ? View.Size.Height : View.Size.Width;
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

        var orientationValue = View.GetStyle(ScrollBar.OrientationProperty);
        var trackSize = orientationValue == Base.Orientation.Vertical ? View.Size.Height : View.Size.Width;
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

        if (orientationValue == Base.Orientation.Vertical)
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

        var thumbMinSize = View.GetStyle(ThumbMinSize);
        var orientationValue = View.GetStyle(ScrollBar.OrientationProperty);
        var trackSize = orientationValue == Base.Orientation.Vertical ? View.Size.Height : View.Size.Width;

        var thumbRatio = data.ViewportSize / range;
        var thumbSize = Math.Max(thumbMinSize, thumbRatio * trackSize);
        return Math.Min(thumbSize, trackSize);
    }

    void OnPointerDown(object? sender, PointerEvent e)
    {
        var thumbRect = CalculateThumbRect();

        if (thumbRect.Contains(e.Position))
        {
            // Start thumb drag
            _isDragging = true;
            _dragStartValue = DataContext.Value;
            _dragStartPoint = e.Position;
            View.SetProperty(Panel.IsPressed, true);
        }
        else
        {
            // Clicked on track - page up/down
            var data = DataContext;
            var largeChange = DataContext.LargeChange;
            var orientationValue = View.GetStyle(ScrollBar.OrientationProperty);



            bool clickedBefore = orientationValue == Base.Orientation.Vertical
                ? e.Position.Y < thumbRect.Y 
                : e.Position.X < thumbRect.X;

            var direction = clickedBefore ? -1 : 1;
            data.Value = Math.Clamp(
                data.Value + direction * largeChange,
                data.Minimum,
                data.Maximum
            );
        }
    }

    void OnPointerMove(object? sender, PointerEvent e)
    {
        if (!_isDragging)
            return;

        var data = DataContext;
        var orientationValue = View.GetStyle(ScrollBar.OrientationProperty);
        var delta = orientationValue == Base.Orientation.Vertical 
            ? e.Position.Y - _dragStartPoint.Y 
            : e.Position.X - _dragStartPoint.X;

        var thumbSize = CalculateThumbSize();
        var trackSize = orientationValue == Base.Orientation.Vertical ? View.Size.Height : View.Size.Width;
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
        }
    }

    void OnPointerCaptureLost(object? sender, EventArgs e)
    {
        _isDragging = false;
        View.SetProperty(Panel.IsPressed, false);
    }
}

