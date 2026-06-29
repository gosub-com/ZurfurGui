using System.Diagnostics;
using ZurfurGui.Base;
using ZurfurGui.Platform;

namespace ZurfurGui.Render;


public class RenderContext
{
    static readonly Rect EMPTY_DEVICE_CLIP = new Rect(0, 0, double.MaxValue, double.MaxValue);

    public struct Stats
    {
        public long FillText;
        public long FillRect;
        public long StrokeRect;
        public long StrokePolyLine;
        public long FillPolygon;
        public long PushClips;
    }

    Stats _renderStats;
    OsContext _context;
    View? _currentView;
    Point _origin;
    double _scale = 1.0;
    Rect _clip = EMPTY_DEVICE_CLIP;
    List<Rect> _clipStack = new List<Rect>();
    double[] _pointsBuffer = Array.Empty<double>();

    public Stats RenderStats => _renderStats;

    public int ClipLevel => _clipStack.Count;

    /// <summary>
    /// The clipping rectangle in device pixels
    /// </summary>
    public Rect DeviceClip => _clip;

    
    /// <summary>
    /// Current pointer position in device pixels
    /// </summary>
    public Point PointerDevicePosition { get; private set; }

    public RenderContext(OsContext context)
    {
        _context = context;
    }

    public double MeasureTextWidth(string fontName, double fontSize, string text)
    {
        _context.FontName = fontName;
        _context.FontSize = 1000;
        return _context.MeasureTextWidth(text) * 0.001 * fontSize;
    }

    /// <summary>
    /// Set the currently rendering view, and  _origin and _scale.
    /// </summary>
    internal void SetCurrentViewInternal(View? view)
    {
        _currentView = view;
        if (view != null)
        {
            _origin = view.Origin;
            _scale = view.Scale;
        }
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

        _context.FillColor = brush.Color;

        // NOTE: Winforms throws an exception if width or height are 0 or negative. 
        // TBD: Fix winforms driver to match Javascript and then remove this test.
        if (width > 0 && height > 0)
        {
            _renderStats.FillRect++;
            _context.FillRect(_scale * x + _origin.X, _scale * y + _origin.Y, _scale * width, _scale * height, radius);
        }
    }

    public void StrokeRect(in Pen pen, Rect r, double radius = 0)
        => StrokeRect(pen, r.X, r.Y, r.Width, r.Height, radius);

    public void StrokeRect(in Pen pen, double x, double y, double width, double height, double radius = 0)
    {
        if (pen.Brush.Type != BrushType.Solid)
            throw new NotImplementedException("Only BrushType.Solid brushes are supported");
        _context.StrokeColor = pen.Brush.Color;
        _context.LineWidth = _scale * pen.Thickness;

        // NOTE: Winforms throws an exception if width or height are 0 or negative.
        // TBD: Fix winforms driver to match Javascript and then remove this test.
        if (width > 0 && height > 0)
        {
            _renderStats.StrokeRect++;
            _context.StrokeRect(_scale * x + _origin.X, _scale * y + _origin.Y, _scale * width, _scale * height, radius);
        }
    }

    public void FillText(in Font font, in Brush brush, string text, Point p)
        => FillText(font, brush, text, p.X, p.Y);

    public void FillText(in Font font, in Brush brush, string text, double x, double y)
    {
        if (brush.Type != BrushType.Solid)
            throw new NotImplementedException("Only BrushType.Solid brushes are supported");
        _context.FillColor = brush.Color;
        _context.FontName = font.Name;
        _context.FontSize = _scale * font.Size;
        _context.FillText(text, _scale * x + _origin.X, _scale * y + _origin.Y);
        _renderStats.FillText++;
    }

    double[] TransformPoints(ReadOnlySpan<double> points)
    {
        // Expand buffer if needed (amortized allocation)
        if (_pointsBuffer.Length < points.Length)
            _pointsBuffer = new double[Math.Max(points.Length, _pointsBuffer.Length * 2)];

        // Transform points into buffer
        for (int i = 0; i < points.Length; i += 2)
        {
            _pointsBuffer[i] = _scale * points[i] + _origin.X;
            _pointsBuffer[i + 1] = _scale * points[i + 1] + _origin.Y;
        }

        return _pointsBuffer;
    }

    public void StrokePolyLine(Pen pen, ReadOnlySpan<double> points)
    {
        if (points.Length < 4 || points.Length % 2 != 0)
            throw new ArgumentException("Points must contain at least 2 coordinate pairs (4 values) and have even length", nameof(points));

        if (pen.Brush.Type != BrushType.Solid)
            throw new NotImplementedException("Only BrushType.Solid brushes are supported");

        _context.StrokeColor = pen.Brush.Color;
        _context.LineWidth = _scale * pen.Thickness;

        var transformed = TransformPoints(points);
        _context.StrokePolyLine(transformed, points.Length);
        _renderStats.StrokePolyLine++;
    }

    public void FillPolygon(Brush brush, ReadOnlySpan<double> points)
    {
        if (points.Length < 6 || points.Length % 2 != 0)
            throw new ArgumentException("Points must contain at least 3 coordinate pairs (6 values) and have even length", nameof(points));

        if (brush.Type != BrushType.Solid)
            throw new NotImplementedException("Only BrushType.Solid brushes are supported");

        _context.FillColor = brush.Color;

        var transformed = TransformPoints(points);
        _context.FillPolygon(transformed, points.Length);
        _renderStats.FillPolygon++;
    }


    /// <summary>
    /// Clip to the specified rectangle in device pixels.
    /// Saves clip state to a stack. Restore is automatic and doesn't need to be done by user code.
    /// </summary>
    public void PushDeviceClip(Rect clip)
    { 
        if (_currentView == null)
            throw new InvalidOperationException("No current view");
        if (_currentView.PushedContentClip)
            return; // Already pushed

        _renderStats.PushClips++;
        _currentView.PushedContentClip = true;
        _clipStack.Add(_clip);
        _clip = _clip.Intersect(clip);
        _context.Clip(_clip.X, _clip.Y, _clip.Width, _clip.Height);
    }

    /// <summary>
    /// Pops the old clip state from the stack.
    /// </summary>
    internal void PopDeviceClip(View view)
    {
        if (!view.PushedContentClip)
            return;  // Not clipped
        view.PushedContentClip = false;

        if (_clipStack.Count == 0)
        {
            Debug.Assert(false, "UnClip without matching Clip");
            _clip = EMPTY_DEVICE_CLIP;
        }
        else
        {
            _clip = _clipStack[^1];
            _clipStack.RemoveAt(_clipStack.Count - 1);
            if (_clipStack.Count == 0)
                Debug.Assert(_clip == EMPTY_DEVICE_CLIP);
        }
        _context.UnClip();
    }
}
