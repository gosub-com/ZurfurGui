using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Base;
using ZurfurGui.Controls;
using ZurfurGui.Property;
using ZurfurGui.Render;

namespace ZurfurGui.Layout;

public class LayoutText : Layoutable
{
    public string TypeName => "Text";

    Size _lastMeasuredSize;

    public Size MeasureView(View view, MeasureContext measure, Size available)
    {
        // Quick exit if text hasn't changed since last measure
        if (!view.Flags.HasFlag(ViewFlags.Measure))
            return _lastMeasuredSize;

        var font = view.GetStyle(TextView.FontProperty);
        var fontName = font.Name ?? "Arial";
        var fontSize = font.Size.Or(16.0);
        var text = view.GetStyle(TextView.TextProperty);
        var metrics = measure.GetFontMetrics(fontName, fontSize);

        var maxWidth = text.Count == 0 ? 0 : text.Max(line => measure.MeasureTextWidth(fontName, fontSize, line));

        // Height = (lines - 1) * lineHeight + (ascent + descent) for last line
        var height = text.Count == 0 ? 0 
            : (text.Count - 1) * metrics.LineHeight + metrics.Ascent + metrics.Descent;

        _lastMeasuredSize = new Size(maxWidth, height);
        return _lastMeasuredSize;
    }

    public void ArrangeViews(View view, MeasureContext measure)
    {
        if (view.Children.Count != 0)
            throw new InvalidOperationException("Text layout does not support children");
    }


}
