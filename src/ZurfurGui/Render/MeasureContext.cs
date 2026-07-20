using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Controls;
using ZurfurGui.Platform;

using ZurfurGui.Collections;

namespace ZurfurGui.Render;

public class MeasureContext
{
    OsContext _context;
    LruDictionary<FontKey, double> _fontCache = new();
    int _prevCacheTotal = 0;

    record struct FontKey(string FontName, double FontSize, string Text);

    public MeasureContext(OsContext context)
    {
        _context = context; 
    }

    public double MeasureTextWidth(string fontName, double fontSize, string text)
    {
        _prevCacheTotal++;
        var key = new FontKey(fontName, fontSize, text);
        if (_fontCache.TryGetValue(key, out var fontWidth))
            return fontWidth;

        var width = _context.MeasureTextWidth(fontName, fontSize, text);
        _fontCache[key] = width;
        return width;
    }

    /// <summary>
    /// Called at the end of the frame to control font cache size.
    /// </summary>
    internal void FrameDone()
    {
        var maxLru = _prevCacheTotal + 1000;
        while (_fontCache.Count > maxLru)
            _fontCache.RemoveLru();
        _prevCacheTotal = 0;
    }


}
