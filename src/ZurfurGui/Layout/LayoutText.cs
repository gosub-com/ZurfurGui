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
        var maxWidth = text.Count == 0 ? 0 : text.Max(line => measure.MeasureTextWidth(fontName, fontSize, line));
        _lastMeasuredSize = new Size(maxWidth, text.Count * TextView.LINE_SPACING * fontSize);
        return _lastMeasuredSize;
    }

    public void ArrangeViews(View view, MeasureContext measure)
    {
        if (view.Children.Count != 0)
            throw new InvalidOperationException("Text layout does not support children");
    }


}
