using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Base;
using ZurfurGui.Platform;

namespace ZurfurGui.Draw;


public class DrawContext
{
    OsContext _context;
    public Point _origin;
    public double _scale = 1.0;
    public int ClipLevel { get; private set; }

    // TBD: Turn into a class, PointerInfo
    public Point PointerPosition { get; private set; }

    public DrawContext(OsContext context)
    {
        _context = context;
    }

    public double MeasureTextWidth(string fontName, double fontSize, string text)
    {
        _context.FontName = fontName;
        _context.FontSize = 1000;
        return _context.MeasureTextWidth(text) * 0.001 * fontSize;
    }

    internal void SetOrigin(Point origin, double scale)
    {
        _origin = origin;
        _scale = scale;
    }

    internal void SetPointerPosition(Point pos)
    {
        PointerPosition = pos;
    }

    public Color FillColor { set { _context.FillColor = value; } }
    public Color StrokeColor { set { _context.StrokeColor = value; } }
    public double LineWidth { set { _context.LineWidth = _scale * value; } }
    public string FontName { set { _context.FontName = value; } }
    public double FontSize { set { _context.FontSize = _scale * value; } }
    public void FillRect(double x, double y, double width, double height, double radius = 0)
    {
        // NOTE: Winforms throws an exception if width or height are 0 or negative. 
        // TBD: Fix winforms driver to match Javascript and then remove this test.
        if (width > 0 && height > 0)
            _context.FillRect(_scale * x + _origin.X, _scale * y + _origin.Y, _scale * width, _scale * height, radius);
    }
    public void FillRect(Rect r, double radius = 0)
        => FillRect(r.X, r.Y, r.Width, r.Height, radius);

    public void StrokeRect(double x, double y, double width, double height, double radius = 0)
    {
        // NOTE: Winforms throws an exception if width or height are 0 or negative.
        // TBD: Fix winforms driver to match Javascript and then remove this test.
        if (width > 0 && height > 0)
            _context.StrokeRect(_scale * x + _origin.X, _scale * y + _origin.Y, _scale * width, _scale * height, radius);
    }
    public void StrokeRect(Rect r, double radius = 0 )
        => StrokeRect(r.X, r.Y, r.Width, r.Height, radius);

    public void FillText(string text, double x, double y)
        => _context.FillText(text, _scale * x + _origin.X, _scale * y + _origin.Y);

    public void FillText(string text, Point p)
        => _context.FillText(text, p.X, p.Y);

    /// <summary>
    /// Clip to the specified rectangle in device pixels (ignoring Origin and Scale).
    /// Saves clip state to a stack.  Use UnClip to restore previous state.
    /// </summary>
    internal void ClipDevice(double x, double y, double width, double height)
    { 
        ClipLevel++;
        _context.Clip(x, y, width, height);
    }

    /// <summary>
    /// Pops the old clip state from the stack.
    /// </summary>
    internal void UnClip()
    { 
        ClipLevel--;
        Debug.Assert(ClipLevel >= 0);
        _context.UnClip();
    }
}
