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
    int _pushedClips;

    public RenderContextStats RenderStats => _stats;

    
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
            _drawBuffer.SetFillColor(brush.Color);
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
            _drawBuffer.SetStrokeColorWidth(pen.Brush.Color, pen.Thickness);
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
        _drawBuffer.SetFillColor(brush.Color);
        _drawBuffer.SetFontNameSize(font.Name, font.Size);
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
        _drawBuffer.SetStrokeColorWidth(pen.Brush.Color, pen.Thickness);
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
        _drawBuffer.SetFillColor(brush.Color);
        _drawBuffer.FillPolygon(points);
    }

    /// <summary>
    /// Clip to the specified rectangle.
    /// Saves clip state to a stack. Restore is automatic and doesn't need to be done by user code.
    /// </summary>
    public void PushClip(Rect clientClip)
    { 
        _stats.PushClips++;
        _pushedClips++;
        _drawBuffer.Clip(clientClip.X, clientClip.Y, clientClip.Width, clientClip.Height);
    }

    /// <summary>
    /// TBD: Keep track of clips separately, NOTE: Clipping disabled
    /// Pops the old clip state from the stack.
    /// </summary>
    internal void PopClip()
    {
        if (_pushedClips <= 0)
        {
            Debug.Assert(false, "UnClip without matching Clip");
            return; 
        }
        _pushedClips--;


        // Send to draw buffer
        _drawBuffer.PopClip();
    }


    internal void FlushClips()
    {
        while (_pushedClips > 0)
        {
            PopClip();
        }
    }

}
