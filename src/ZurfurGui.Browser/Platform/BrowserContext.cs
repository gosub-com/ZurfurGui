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

    [JSImport("globalThis.ZurfurGui.measureText")]
    private static partial JSObject MeasureText(JSObject context, string font, string text);

    [JSImport("globalThis.ZurfurGui.getFontMetrics")]
    private static partial JSObject GetFontMetrics(JSObject context, string font);

    [JSImport("globalThis.ZurfurGui.marshalString")]
    static partial void MarshalString(string? str, int index);

    [JSImport("globalThis.ZurfurGui.present")]
    static partial void Present(JSObject context, double[] buffer, int length);

    JSObject _context;

    Dictionary<int, string> _marshaledStrings = new();
    
    public BrowserContext(JSObject context)
    {
        _context = context;
    }


    double OsContext.MeasureTextWidth(string fontName, double fontSize, string text)
    {
        var font = $"{fontSize}px {fontName}";
        return MeasureText(_context, font, text).GetPropertyAsDouble("width");
    }

    FontMetrics OsContext.GetFontMetrics(string fontName, double fontSize)
    {
        var font = $"{fontSize}px {fontName}";
        var metrics = GetFontMetrics(_context, font);
        var ascent = metrics.GetPropertyAsDouble("ascent");
        var descent = metrics.GetPropertyAsDouble("descent");
        var lineHeight = metrics.GetPropertyAsDouble("lineHeight");
        return new FontMetrics(ascent, descent, lineHeight);
    }

    void OsContext.MarshalString(string? str, int index)
    {
        if (str == null)
            _marshaledStrings.Remove(index);
        else
            _marshaledStrings[index] = str;
        MarshalString(str, index);
    }

    void OsContext.Present(OsRenderBuffer renderBuffer)
    {
        Present(_context, renderBuffer.Commands, renderBuffer.CommandsLength);
    }

}
