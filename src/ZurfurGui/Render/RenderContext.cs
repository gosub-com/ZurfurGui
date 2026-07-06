using System.Diagnostics;
using ZurfurGui.Base;
using ZurfurGui.Platform;

namespace ZurfurGui.Render;


public class RenderContext
{
    static readonly Rect EMPTY_DEVICE_CLIP = new Rect(0, 0, double.MaxValue, double.MaxValue);

    public struct RenderContextStats
    {
        public long FillText;
        public long FillRect;
        public long StrokeRect;
        public long StrokePolyLine;
        public long FillPolygon;
        public long PushClips;

        public static RenderContextStats operator -(RenderContextStats a, RenderContextStats b)
        {
            return new RenderContextStats
            {
                FillText = a.FillText - b.FillText,
                FillRect = a.FillRect - b.FillRect,
                StrokeRect = a.StrokeRect - b.StrokeRect,
                StrokePolyLine = a.StrokePolyLine - b.StrokePolyLine,
                FillPolygon = a.FillPolygon - b.FillPolygon,
                PushClips = a.PushClips - b.PushClips
            };
        }

    }

    RenderContextStats _stats;
    OsContext _context;
    OsDrawBuffer _drawBuffer;
    View? _currentView;
    Rect _deviceClip = EMPTY_DEVICE_CLIP;
    Stack<Rect> _deviceClipStack = new();

    public RenderContextStats RenderStats => _stats;

    public int ClipLevel => _deviceClipStack.Count;

    /// <summary>
    /// The clipping rectangle in device pixels
    /// </summary>
    public Rect DeviceClip => _deviceClip;

    
    /// <summary>
    /// Current pointer position in device pixels
    /// </summary>
    public Point PointerDevicePosition { get; private set; }

    internal RenderContext(OsContext context, OsDrawBuffer drawBuffer)
    {
        _context = context;
        _drawBuffer = drawBuffer;
    }

    public double MeasureTextWidth(string fontName, double fontSize, string text)
    {
        return _context.MeasureTextWidth(fontName, fontSize, text) * 0.001 * fontSize;
    }

    internal void SetPointerPosition(Point devicePosition)
    {
        PointerDevicePosition = devicePosition;
    }

    public void FillRect(Brush brush, Rect r, double radius = 0)
        => FillRect(brush, r.X, r.Y, r.Width, r.Height, radius);

    public void FillRect(in Brush brush, double x, double y, double width, double height, double radius = 0)
    {
        if (brush.Type != BrushType.Solid)
            throw new NotImplementedException("Only BrushType.Solid brushes are supported");

        // Send to draw buffer
        if (width > 0 && height > 0)
        {
            _stats.FillRect++;
            _drawBuffer.FillColor(brush.Color);
            _drawBuffer.FillRect(x, y, width, height, radius);
        }
    }

    public void StrokeRect(in Pen pen, Rect r, double radius = 0)
        => StrokeRect(pen, r.X, r.Y, r.Width, r.Height, radius);

    public void StrokeRect(in Pen pen, double x, double y, double width, double height, double radius = 0)
    {
        if (pen.Brush.Type != BrushType.Solid)
            throw new NotImplementedException("Only BrushType.Solid brushes are supported");

        // Send to draw buffer
        if (width > 0 && height > 0)
        {
            _stats.StrokeRect++;
            _drawBuffer.StrokeColor(pen.Brush.Color);
            _drawBuffer.LineWidth(pen.Thickness);
            _drawBuffer.StrokeRect(x, y, width, height, radius);
        }
    }

    public void FillText(in Font font, in Brush brush, string text, Point p)
        => FillText(font, brush, text, p.X, p.Y);

    public void FillText(in Font font, in Brush brush, string text, double x, double y)
    {
        if (brush.Type != BrushType.Solid)
            throw new NotImplementedException("Only BrushType.Solid brushes are supported");

        // Send to draw buffer
        _stats.FillText++;
        _drawBuffer.FillColor(brush.Color);
        _drawBuffer.FontName(font.Name, font.Size);
        _drawBuffer.FillText(text, x, y);
    }

    public void StrokePolyLine(Pen pen, ReadOnlySpan<double> points)
    {
        if (points.Length < 4 || points.Length % 2 != 0)
            throw new ArgumentException("Points must contain at least 2 coordinate pairs (4 values) and have even length", nameof(points));

        if (pen.Brush.Type != BrushType.Solid)
            throw new NotImplementedException("Only BrushType.Solid brushes are supported");

        // Send to draw buffer
        _stats.StrokePolyLine++;
        _drawBuffer.StrokeColor(pen.Brush.Color);
        _drawBuffer.LineWidth(pen.Thickness);
        _drawBuffer.StrokePolyLine(points);
    }

    public void FillPolygon(Brush brush, ReadOnlySpan<double> points)
    {
        if (points.Length < 6 || points.Length % 2 != 0)
            throw new ArgumentException("Points must contain at least 3 coordinate pairs (6 values) and have even length", nameof(points));

        if (brush.Type != BrushType.Solid)
            throw new NotImplementedException("Only BrushType.Solid brushes are supported");

        // Send to draw buffer
        _stats.FillPolygon++;
        _drawBuffer.FillColor(brush.Color);
        _drawBuffer.FillPolygon(points);
    }

    /// <summary>
    /// Set the currently rendering view.  This is used to stash clip info in the view.
    /// </summary>
    internal void SetCurrentViewInternal(View? view)
    {
        _currentView = view;
    }

    /// <summary>
    /// Clip to the specified rectangle in device pixels.
    /// Saves clip state to a stack. Restore is automatic and doesn't need to be done by user code.
    /// </summary>
    public void PushClip(Rect clientClip)
    { 
        if (_currentView == null)
            throw new InvalidOperationException("No current view");

        var deviceClip = _currentView.toDevice(clientClip);

        _stats.PushClips++;
        _currentView._pushedClips++;
        _deviceClipStack.Push(_deviceClip);
        _deviceClip = _deviceClip.Intersect(deviceClip);

        var newClientClip = _currentView.toClient(_deviceClip);

        // Send to draw buffer
        _drawBuffer.Clip(newClientClip.X, newClientClip.Y, newClientClip.Width, newClientClip.Height);
    }

    /// <summary>
    /// Pops the old clip state from the stack.
    /// </summary>
    internal void PopClip(View view)
    {
        if (view._pushedClips <= 0)
        {
            Debug.Assert(false, "UnClip without matching Clip");
            return; 
        }
        view._pushedClips--;

        if (_deviceClipStack.Count == 0)
        {
            Debug.Assert(false, "UnClip without matching Clip");
            _deviceClip = EMPTY_DEVICE_CLIP;
        }
        else
        {
            _deviceClip = _deviceClipStack.Pop();
            if (_deviceClipStack.Count == 0)
                Debug.Assert(_deviceClip == EMPTY_DEVICE_CLIP);
        }

        // Send to draw buffer
        _drawBuffer.UnClip();
    }

    internal void ClearClips()
    {
        while (_deviceClipStack.Count > 0)
        {
            Debug.Assert(false, "Leftover Clips after rendering");
            _deviceClipStack.Pop();
            _drawBuffer.UnClip();
        }
    }

}
