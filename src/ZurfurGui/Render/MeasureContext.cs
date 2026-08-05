using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Base;
using ZurfurGui.Controls;
using ZurfurGui.Platform;

using ZurfurGui.Collections;

namespace ZurfurGui.Render;

public class MeasureContext
{
    OsContext _context;
    LruDictionary<FontKey, double> _fontCache = new();
    LruDictionary<MetricsKey, FontMetrics> _metricsCache = new();
    int _prevFontCacheTotal = 0;
    int _prevMetricsCacheTotal = 0;

    record struct FontKey(string FontName, double FontSize, string Text);
    record struct MetricsKey(string FontName, double FontSize);

    public MeasureContext(OsContext context)
    {
        _context = context; 
    }

    public double MeasureTextWidth(string fontName, double fontSize, string text)
    {
        _prevFontCacheTotal++;
        var key = new FontKey(fontName, fontSize, text);
        if (_fontCache.TryGetValue(key, out var fontWidth))
            return fontWidth;

        var width = _context.MeasureTextWidth(fontName, fontSize, text);
        _fontCache[key] = width;
        return width;
    }

    public FontMetrics GetFontMetrics(string fontName, double fontSize)
    {
        _prevMetricsCacheTotal++;
        var key = new MetricsKey(fontName, fontSize);
        if (_metricsCache.TryGetValue(key, out var metrics))
            return metrics;

        metrics = _context.GetFontMetrics(fontName, fontSize);
        _metricsCache[key] = metrics;
        return metrics;
    }

    /// <summary>
    /// Called at the end of the frame to control font cache size.
    /// </summary>
    internal void FrameDone()
    {
        var maxFontLru = _prevFontCacheTotal + 1000;
        while (_fontCache.Count > maxFontLru)
            _fontCache.RemoveLru();

        var maxMetricsLru = _prevMetricsCacheTotal + 100;
        while (_metricsCache.Count > maxMetricsLru)
            _metricsCache.RemoveLru();

        _prevFontCacheTotal = 0;
        _prevMetricsCacheTotal = 0;
    }


}
