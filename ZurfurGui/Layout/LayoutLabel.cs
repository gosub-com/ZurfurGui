using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Controls;

namespace ZurfurGui.Layout;

public class LayoutLabel : Layoutable
{
    /// <summary>
    /// Since there is no state, we can use a single instance for all text
    /// </summary>
    public static readonly LayoutLabel Instance = new();

    public string LayoutType => "Text";

    public Size MeasureView(View view, MeasureContext measure, Size available)
    {
        return MeasureLabel(measure, view);
    }


    static Size MeasureLabel(MeasureContext measure, View view)
    {
        var fontName = view.Properties.Get(Zui.FontName) ?? "Arial";
        var fontSize = view.Properties.Get(Zui.FontSize, 16.0);
        var text = view.Properties.Get(Zui.Text) ?? "";
        var lines = text.Split("\n");
        var maxWidth = lines.Max(line => measure.MeasureTextWidth(fontName, fontSize, line));
        return new Size(maxWidth, lines.Length * Label.LINE_SPACING * fontSize);
    }

    public Size ArrangeViews(View view, MeasureContext measure, Size final, Rect contentRect)
    {
        return final;
    }


}
