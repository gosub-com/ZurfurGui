using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Platform;

namespace ZurfurGui.Browser.Interop;



internal partial class BrowserContext : OsContext
{
    [JSImport("globalThis.ZurfurGui.fillRect")]
    private static partial void FillRect(JSObject context, double x, double y, double width, double height);

    [JSImport("globalThis.ZurfurGui.fillText")]
    private static partial void FillText(JSObject context, string text, double x, double y);

    [JSImport("globalThis.ZurfurGui.measureText")]
    private static partial JSObject MeasureText(JSObject context, string text);

    JSObject _context;
    double _pixelScale = 1;
    double _fontSize = 16;
    string _font = "sans-serif";
    string? _fontString;
    
    public BrowserContext(JSObject context)
    {
        _context = context;
        _context.SetProperty("textBaseline", "alphabetic");
        Console.WriteLine("Made context");
    }

    public double PixelScale
    {
        get => _pixelScale;
        set
        {
            if (_pixelScale != value)
            {
                _pixelScale = value;
                _fontString = null;
            }
        }
    }

    public void FillRect(double x, double y, double width, double height)
        => FillRect(_context, x*_pixelScale, y*_pixelScale, width*_pixelScale, height*_pixelScale);

    public void FillText(string text, double x, double y)
    {
        SetFontString();
        FillText(_context, text, x*_pixelScale, y*_pixelScale);
    }

    void SetFontString()
    {
        if (_fontString == null)
        {
            _fontString = $"{_fontSize*_pixelScale}px {_font}";
            _context.SetProperty("font", _fontString);
        }
    }

    public Color FillColor
    {
        set => _context.SetProperty("fillStyle", value.CssColor);
    }

    public string Font
    {
        get => _font;
        set
        {
            if (value != _font)
            {
                _font = value;
                _fontString = null;
            }
        }
    }

    public double FontSize
    {
        get => _fontSize;
        set
        {
            if (_fontSize != value)
            {
                _fontSize = value;
                _fontString = null;
            }
        }
    }

    public double MeasureTextWidth(string text)
    {
        SetFontString();
        return MeasureText(_context, text).GetPropertyAsDouble("width")/_pixelScale;
    }

}
