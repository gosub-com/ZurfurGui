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

    [JSImport("globalThis.ZurfurGui.marshalString")]
    static partial void MarshalString(string? str, int index);

    [JSImport("globalThis.ZurfurGui.drawBuffer")]
    static partial void DrawBuffer(JSObject context, double[] buffer, int length);

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

    void OsContext.MarshalString(string? str, int index)
    {
        if (str == null)
            _marshaledStrings.Remove(index);
        else
            _marshaledStrings[index] = str;
        MarshalString(str, index);
    }

    void OsContext.DrawBuffer(OsDrawBuffer drawBuffer)
    {
        DrawBuffer(_context, drawBuffer.Commands, drawBuffer.CommandsLength);
    }

}
