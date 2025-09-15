using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Drawing2D;

using ZurfurGui.Platform;

using Color = ZurfurGui.Base.Color;

namespace ZurfurGui.WinForms.Interop;



/// <summary>
/// For now, use Winforms Graphics and try and make it look the same as Chrome.
/// Chrome uses Skia, so investigate matching the browsuer with Skia.
/// </summary>
internal class WinContext : OsContext
{

    /// <summary>
    /// Winforms uses points, so we scale to pixels
    /// </summary>
    const float PIXEL_TO_POINT = 72.0f / 96;

    /// <summary>
    /// Fudge factor to make Winforms text offset the same as the browser canvas
    /// </summary>
    const double TEXT_OFFSET_SCALE_Y = 0.075;
    const double TEXT_OFFSET_SCALE_X = 0.175;

    static System.Drawing.Color WinColor(Color c)
        => System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B);


    public Graphics _graphics;
    Brush? _brush;
    Pen? _pen;
    Font? _font;
    GraphicsPath _path = new();
    Region _region = new();

    Color _fillColor = new Color(0, 0, 0);
    Color _strokeColor = new Color(0, 0, 0);
    double _lineWidth = 1;
    string _fontName = "Arial";
    double _fontSize = 16;
    RectangleF _currentClip = new RectangleF(-1000000, -1000000, 2000000, 2000000);
    List<RectangleF> _clipStack = new();


    public WinContext(Graphics graphics)
    {
        _graphics = graphics;
    }

    Font GetFont()
    {
        if (_font == null)
            _font = new Font(_fontName, (float)(_fontSize * PIXEL_TO_POINT));
        return _font;
    }

    Brush GetBrush()
    {
        if (_brush == null)
            _brush = new SolidBrush(WinColor(_fillColor));
        return _brush;
    }

    Pen GetPen()
    {
        if (_pen == null)
            _pen = new Pen(WinColor(_strokeColor), (float)(_lineWidth));
        return _pen;
    }

    public Color FillColor 
    {
        get => _fillColor;
        set 
        { 
            if (value != _fillColor) 
            {
                _fillColor = value; 
                _brush = null; 
            } 
        } 
    }
    public Color StrokeColor
    {
        get => _strokeColor;
        set
        {
            if (value != _strokeColor)
            {
                _strokeColor = value;
                _pen = null;
            }
        }
    }

    public double LineWidth
    {
        get => _lineWidth;
        set
        {
            if (value != _lineWidth)
            {
                _lineWidth = value;
                _pen = null;
            }
        }
    }

    public string FontName 
    {
        get => _fontName;
        set 
        {
            if (_fontName != value)
            {
                _fontName = value;
                _font = null;
            }
        } 
    }

    public double FontSize
    {
        get => _fontSize;
        set
        {
            if (value != _fontSize)
            {
                _fontSize = value;
                _font = null;
            }
        }
    }

    static GraphicsPath GetRoundedRect(double x, double y, double width, double height, double radius)
    {
        var path = new GraphicsPath();
        var diameter = Math.Min(radius * 2, Math.Min(height, width));
        var arc = new RectangleF((float)x, (float)y, (float)diameter, (float)diameter);
        path.AddArc(arc, 180, 90); // top left
        arc.X = (float)(x + width - diameter);
        path.AddArc(arc, 270, 90); // top right
        arc.Y = (float)(y + height - diameter);
        path.AddArc(arc, 0, 90); // bottom right
        arc.X = (float)x;
        path.AddArc(arc, 90, 90); // bottom left
        path.CloseFigure();
        return path;
    }

    public void FillRect(double x, double y, double width, double height, double radius)
    {
        if (radius > 0)
        {
            using var path = GetRoundedRect(x, y, width, height, radius);
            _graphics.FillPath(GetBrush(), path);
        }
        else
        {
            _graphics.FillRectangle(GetBrush(), (float)x, (float)y, (float)width, (float)height);
        }
    }

    public void StrokeRect(double x, double y, double width, double height, double radius)
    {
        if (radius > 0)
        {
            using var path = GetRoundedRect(x, y, width, height, radius);
            _graphics.DrawPath(GetPen(), path);
        }
        else
        {
            _graphics.DrawRectangle(GetPen(), (float)x, (float)y, (float)width, (float)height);
        }
    }

    public void FillText(string text, double x, double y)
    {
        y -= _fontSize;  // Draw at alphabetic base line
        y += _fontSize * TEXT_OFFSET_SCALE_Y;
        x -= _fontSize * TEXT_OFFSET_SCALE_X;
        _graphics.DrawString(text, GetFont(), GetBrush(), (float)x, (float)y);
    }

    public double MeasureTextWidth(string text)
    {
        return TextRenderer.MeasureText(text, GetFont(), new System.Drawing.Size(0,0), 
            TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Width;
    }

    /// <summary>
    /// Clip to the specified rectangle in device pixels
    /// </summary>
    public void Clip(double x, double y, double width, double height)
    {
        _clipStack.Add(_currentClip);
        _currentClip = new RectangleF((float)x, (float)y, (float)width, (float)height);

        _region.MakeInfinite();
        _region.Intersect(_currentClip);
        _graphics.Clip = _region;
    }

    public void UnClip()
    {
        _currentClip = new RectangleF(-1000000, -1000000, 2000000, 2000000);
        if (_clipStack.Count > 0)
        {
            _currentClip = _clipStack[^1];
            _clipStack.RemoveAt(_clipStack.Count - 1);
        }

        _region.MakeInfinite();
        _region.Intersect(_currentClip);
        _graphics.Clip = _region;

    }
}
