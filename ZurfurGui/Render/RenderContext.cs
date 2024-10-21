using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Platform;

namespace ZurfurGui.Render;

public class RenderContext
{
    OsContext _context;

    Point _origin;
    double _scale = 1.0;

    List<(Point origin, double scale)> Origins = new();

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

    public void PushOrigin(Point origin, double scale)
    {
        Origins.Add((_origin, _scale));
        _scale *= scale;
        _origin = _origin + _scale*origin;
    }

    public void PopOrigin()
    {
        if (Origins.Count == 0)
            throw new Exception("Pop origin failed");
        (_origin, _scale) = Origins[^1];
        Origins.RemoveAt(Origins.Count - 1);
    }


    public Color FillColor { set { _context.FillColor = value; } }
    public Color StrokeColor { set { _context.StrokeColor = value; } }
    public double LineWidth { set { _context.LineWidth = _scale * value; } }
    public string FontName { set { _context.FontName = value; } }
    public double FontSize { set { _context.FontSize = _scale * value; } }
    public void FillRect(double x, double y, double width, double height)
        => _context.FillRect(_scale * x + _origin.X, _scale * y + _origin.Y, _scale * width, _scale * height);
    public void FillRect(Rect r)
        => FillRect(r.X, r.Y, r.Width, r.Height);
    public void StrokeRect(double x, double y, double width, double height)
        => _context.StrokeRect(_scale * x + _origin.X, _scale * y + _origin.Y, _scale * width, _scale * height);
    public void StrokeRect(Rect r)
        => StrokeRect(r.X, r.Y, r.Width, r.Height);
    public void FillText(string text, double x, double y)
        => _context.FillText(text, _scale * x + _origin.X, _scale * y + _origin.Y);
    public void FillText(string text, Point p)
        => _context.FillText(text, p.X, p.Y);
    public void ClipRect(double x, double y, double width, double height)
        => _context.ClipRect(_scale * x + _origin.X, _scale * y + _origin.Y, _scale * width, _scale * height);
    public void ClipRect(Rect r)
        => ClipRect(r.X, r.Y, r.Width, r.Height);


}
