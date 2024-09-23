using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Platform;

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

    /// <summary>
    /// Fudge factor to get Winforms to measure the text the same as the browser
    /// </summary>
    const float TEXT_WIDTH_SCALE = 0.88f;



    public Graphics _graphics;
    double _pixelScale = 1;
    Color _color;
    Brush? _brush;
    Font? _font;
    string _fontName = "Arial";
    double _fontSize = 26;

    public WinContext(Graphics graphics)
    {
        _graphics = graphics;
    }

    public double PixelScale 
    { 
        get => _pixelScale; 
        set
        {
            if (_pixelScale != value)
            {
                _pixelScale = value;
                _font = null;
            }
        }
    }

    public Color FillColor 
    {
        get => _color;
        set 
        { 
            if (value != _color) 
            {
                _color = value; 
                _brush = null; 
            } 
        } 
    }
    
    public string Font 
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

    Font GetFont()
    {
        if (_font == null)
            _font = new Font(_fontName, (float)(_fontSize * PixelScale * PIXEL_TO_POINT));
        return _font;
    }

    Brush GetBrush()
    {
        if (_brush == null)
            _brush = new SolidBrush(System.Drawing.Color.FromArgb(_color.A, _color.R, _color.G, _color.B));
        return _brush;
    }

    public void FillRect(double x, double y, double width, double height)
    {
        _graphics.FillRectangle(GetBrush(), 
            (float)(x*_pixelScale), (float)(y*_pixelScale), (float)(width*_pixelScale), (float)(height*_pixelScale));
    }

    public void FillText(string text, double x, double y)
    {
        y -= _fontSize;  // Draw at alphabetic base line
        y += _fontSize * TEXT_OFFSET_SCALE_Y;
        x -= _fontSize * TEXT_OFFSET_SCALE_X;
        _graphics.DrawString(text, GetFont(), GetBrush(), (float)(x*_pixelScale), (float)(y*_pixelScale));
    }

    public double MeasureTextWidth(string text)
    {
        return _graphics.MeasureString(text, GetFont()).Width* TEXT_WIDTH_SCALE / _pixelScale;
    }
}
