using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Base;
using ZurfurGui.Controls;
using ZurfurGui.Layout;

namespace ZurfurGui.Draw;
public class DrawLabel : Drawable
{
    public const double LINE_SPACING = 1.2;
    const double TEXT_BASELINE = 0.8; // Expand lines over the size of the font itself

    /// <summary>
    /// Since there is no state, we can use a single instance for all text
    /// </summary>
    public static readonly DrawLabel Instance = new();


    public string DrawType => "Text";

    public void Draw(View view, DrawContext context, Rect contentRect)
    {
        var color = view.PointerHoverTarget ? Colors.Red : Colors.White;
        Draw(view, context, contentRect, color);
    }

    public bool IsHit(View view, Point point)
    {
        var p = view.toClient(point);
        return new Rect(new(0, 0), view.DesiredSize).Contains(p);
    }

    public static void Draw(View view, DrawContext context, Rect contentRect, Color color)
    {
        var text = view.GetStyle(Zui.Text, "�");
        var fontName = view.GetStyle(Zui.FontName, "Arial");
        var fontSize = view.GetStyle(Zui.FontSize, 16.0).Or(16.0);
        context.FontName = fontName;
        context.FontSize = fontSize;
        context.FillColor = color;

        for (int i = 0; i < text.Count; i++)
            context.FillText(text[i], 0 + contentRect.X, 
                fontSize * TEXT_BASELINE + i * fontSize * LINE_SPACING + (LINE_SPACING - 1) * fontSize * 0.5 + contentRect.Y);
    }

}
