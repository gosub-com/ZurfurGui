using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Base;
using ZurfurGui.Platform;

namespace ZurfurGui.Browser.Interop;



internal partial class BrowserContext : OsContext
{
    [JSImport("globalThis.ZurfurGui.fillRect")]
    private static partial void FillRect(JSObject context, double x, double y, double width, double height, double radius);

    [JSImport("globalThis.ZurfurGui.strokeRect")]
    private static partial void StrokeRect(JSObject context, double x, double y, double width, double height, double radius);

    [JSImport("globalThis.ZurfurGui.fillText")]
    private static partial void FillText(JSObject context, string text, double x, double y);

    [JSImport("globalThis.ZurfurGui.measureText")]
    private static partial JSObject MeasureText(JSObject context, string text);

    [JSImport("globalThis.ZurfurGui.clipRect")]
    private static partial void Clip(JSObject context, double x, double y, double width, double height);

    [JSImport("globalThis.ZurfurGui.unClip")]
    private static partial void UnClip(JSObject context);

    JSObject _context;


    string? _fontString;
    string? _fillStyleString;
    string? _strokeStyleString;
    bool _lineWidthInvalid = true;

    Color _fillColor = new Color(0, 0, 0);
    Color _strokeColor = new Color(0, 0, 0);
    double _lineWidth = 1;
    string _fontName = "sans-serif";
    double _fontSize = 16;

    
    public BrowserContext(JSObject context)
    {
        _context = context;
    }

    public Color FillColor
    {
        get => _fillColor;
        set
        {
            if (value != _fillColor)
            {
                _fillColor = value;
                _fillStyleString = null;
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
                _strokeStyleString = null;
            }
        }
    }

    public double LineWidth
    {
        get => _lineWidth;
        set
        {
            if (_lineWidth != value)
            {
                _lineWidth = value;
                _context.SetProperty("lineWidth", _lineWidth);
            }
        }
    }

    public string FontName
    {
        get => _fontName;
        set
        {
            if (value != _fontName)
            {
                _fontName = value;
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

    void ReconstructFontString()
    {
        if (_fontString == null)
        {
            _fontString = $"{_fontSize}px {_fontName}";
            _context.SetProperty("font", _fontString);
        }
    }

    void ReconstructFillStyleString()
    {
        if (_fillStyleString == null)
        {
            _fillStyleString = _fillColor.CssColor;
            _context.SetProperty("fillStyle", _fillColor.CssColor);
        }
    }
    void ReconstructStrokeStyleString()
    {
        if (_strokeStyleString == null)
        {
            _strokeStyleString = _strokeColor.CssColor;
            _context.SetProperty("strokeStyle", _strokeColor.CssColor);
        }
    }

    void ReconstructLineWidth()
    {
        if (_lineWidthInvalid)
        {
            _lineWidthInvalid = false;
            _context.SetProperty("lineWidth", _lineWidth);
        }
    }

    public void FillRect(double x, double y, double width, double height, double radius)
    {
        ReconstructFillStyleString();
        FillRect(_context, x, y, width, height, radius);
    }

    public void StrokeRect(double x, double y, double width, double height, double radius)
    { 
        ReconstructStrokeStyleString();
        ReconstructLineWidth();
        StrokeRect(_context, x, y, width, height, radius);
    }

    public void FillText(string text, double x, double y)
    {
        ReconstructFillStyleString();
        ReconstructFontString();
        FillText(_context, text, x, y);
    }

    public double MeasureTextWidth(string text)
    {
        ReconstructFontString();
        return MeasureText(_context, text).GetPropertyAsDouble("width");
    }

    public void Clip(double x, double y, double width, double height)
    {
        Clip(_context, x, y, width, height);
    }

    public void UnClip()
    {
        UnClip(_context);
        _fontString = null;
        _fillStyleString = null;
        _strokeStyleString = null;
        _lineWidthInvalid = true;

    }

}
