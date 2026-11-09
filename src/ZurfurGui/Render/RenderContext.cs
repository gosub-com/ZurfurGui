using System.Diagnostics;
using ZurfurGui.Base;
using ZurfurGui.Platform;

namespace ZurfurGui.Render;

/// <summary>
/// Records drawing operations for a view during a render pass.
/// </summary>
public class RenderContext
{
    internal struct RenderContextStats
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
    MeasureContext _measureContext;
    OsRenderBuffer _renderBuffer = new();
    int _pushedClips;

    internal RenderContextStats RenderStats => _stats;


    /// <summary>
    /// Gets the measurement context used during this render pass.
    /// </summary>
    public MeasureContext MeasureContext => _measureContext;

    
    /// <summary>
    /// Current pointer position in device pixels
    /// </summary>
    public Point PointerDevicePosition { get; private set; }

    internal RenderContext(MeasureContext measureContext)
    {
        _measureContext = measureContext;
    }

    internal void ClearRenderBuffer() => _renderBuffer.Clear();

    internal OsRenderBuffer CloneRenderBuffer(OsRenderBuffer? maybeInto) => _renderBuffer.Clone(maybeInto);

    internal void SetPointerPosition(Point devicePosition)
    {
        PointerDevicePosition = devicePosition;
    }

    /// <summary>
    /// Fills a rectangle with a brush.
    /// </summary>
    public void FillRect(Brush brush, Rect r, double radius = 0)
        => FillRect(brush, r.X, r.Y, r.Width, r.Height, radius);

    /// <summary>
    /// Fills a rectangle at the specified position and size with a brush.
    /// </summary>
    public void FillRect(in Brush brush, double x, double y, double width, double height, double radius = 0)
    {
        if (brush.Type != BrushType.Solid)
            throw new NotImplementedException("Only BrushType.Solid brushes are supported");

        // Send to render buffer
        if (width > 0 && height > 0)
        {
            _stats.FillRect++;
            _renderBuffer.SetFillColor(brush.Color);
            _renderBuffer.FillRect(x, y, width, height, radius);
        }
    }

    /// <summary>
    /// Draws a rectangle outline with a pen.
    /// </summary>
    public void StrokeRect(in Pen pen, Rect r, double radius = 0)
        => StrokeRect(pen, r.X, r.Y, r.Width, r.Height, radius);

    /// <summary>
    /// Draws a rectangle outline at the specified position and size with a pen.
    /// </summary>
    public void StrokeRect(in Pen pen, double x, double y, double width, double height, double radius = 0)
    {
        if (pen.Brush.Type != BrushType.Solid)
            throw new NotImplementedException("Only BrushType.Solid brushes are supported");

        // Send to render buffer
        if (width > 0 && height > 0)
        {
            _stats.StrokeRect++;
            _renderBuffer.SetStrokeColorWidth(pen.Brush.Color, pen.Thickness);
            _renderBuffer.StrokeRect(x, y, width, height, radius);
        }
    }

    /// <summary>
    /// Draws text with a font and brush at a point.
    /// </summary>
    public void FillText(in Font font, in Brush brush, string text, Point p)
        => FillText(font, brush, text, p.X, p.Y);

    /// <summary>
    /// Draws text with a font and brush at the specified coordinates.
    /// </summary>
    public void FillText(in Font font, in Brush brush, string text, double x, double y)
    {
        if (brush.Type != BrushType.Solid)
            throw new NotImplementedException("Only BrushType.Solid brushes are supported");

        // Send to render buffer
        _stats.FillText++;
        _renderBuffer.SetFillColor(brush.Color);
        _renderBuffer.SetFontNameSize(font.Name, font.Size);
        _renderBuffer.FillText(text, x, y);
    }

    /// <summary>
    /// Draws a connected series of line segments through the specified points.
    /// </summary>
    public void StrokePolyLine(Pen pen, ReadOnlySpan<double> points)
    {
        if (points.Length < 4 || points.Length % 2 != 0)
            throw new ArgumentException("Points must contain at least 2 coordinate pairs (4 values) and have even length", nameof(points));

        if (pen.Brush.Type != BrushType.Solid)
            throw new NotImplementedException("Only BrushType.Solid brushes are supported");

        // Send to render buffer
        _stats.StrokePolyLine++;
        _renderBuffer.SetStrokeColorWidth(pen.Brush.Color, pen.Thickness);
        _renderBuffer.StrokePolyLine(points);
    }

    /// <summary>
    /// Fills a polygon whose vertices are specified by the given coordinates.
    /// </summary>
    public void FillPolygon(Brush brush, ReadOnlySpan<double> points)
    {
        if (points.Length < 6 || points.Length % 2 != 0)
            throw new ArgumentException("Points must contain at least 3 coordinate pairs (6 values) and have even length", nameof(points));

        if (brush.Type != BrushType.Solid)
            throw new NotImplementedException("Only BrushType.Solid brushes are supported");

        // Send to render buffer
        _stats.FillPolygon++;
        _renderBuffer.SetFillColor(brush.Color);
        _renderBuffer.FillPolygon(points);
    }

    /// <summary>
    /// Clip to the specified rectangle.
    /// Saves clip state to a stack. Restore is automatic and doesn't need to be done by user code.
    /// </summary>
    /// <param name="clientClip">The clip rectangle in client coordinates.</param>
    public void PushClip(Rect clientClip)
    { 
        _stats.PushClips++;
        _pushedClips++;
        _renderBuffer.Clip(clientClip.X, clientClip.Y, clientClip.Width, clientClip.Height);
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


        // Send to render buffer
        _renderBuffer.PopClip();
    }


    internal void FlushClips()
    {
        while (_pushedClips > 0)
        {
            PopClip();
        }
    }

}
