using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Base;
using ZurfurGui.Controls;

namespace ZurfurGui.Layout;

public class LayoutText : Layoutable
{
    /// <summary>
    /// Since there is no state, we can use a single instance for all text
    /// </summary>
    public static readonly LayoutText Instance = new();

    public string TypeName => "Text";

    public Size MeasureView(View view, MeasureContext measure, Size available)
    {
        return MeasureLabel(measure, view);
    }


    static Size MeasureLabel(MeasureContext measure, View view)
    {
        var fontName = view.GetStyle(Zui.FontName);
        var fontSize = view.GetStyle(Zui.FontSize).Or(16.0);
        var text = view.GetStyle(Zui.Text);
        var maxWidth = text.Max(line => measure.MeasureTextWidth(fontName, fontSize, line));
        return new Size(maxWidth, text.Count * Text.LINE_SPACING * fontSize);
    }

    public void ArrangeViews(View view, MeasureContext measure)
    {
    }


}
