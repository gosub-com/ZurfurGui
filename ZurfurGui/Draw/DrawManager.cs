using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Base;
using ZurfurGui.Controls;

namespace ZurfurGui.Draw;
public static class DrawManager
{
    const byte ALPHA_HIT_THRESHOLD = 16; // Must be visible to user before it's a hit

    /// <summary>
    /// Draws a panel with a border & background.
    /// </summary>
    public static void Draw(View view, DrawContext context)
    {
        var backgroundRect = new Rect(new(), view.Size);

        // Draw background
        var borderRadius = view.GetStyle(Zui.BorderRadius, null).Or(0);
        var background = view.GetStyle(Zui.Background, null).Or(Colors.TransParent);
        if (background.A != 0)
        {
            context.FillColor = background;
            context.FillRect(backgroundRect, borderRadius);
        }

        // Draw border
        var borderColor = view.GetStyle(Zui.BorderColor, null).Or(Colors.TransParent);
        var borderWidth = view.GetStyle(Zui.BorderWidth, null).Or(0);
        if (borderWidth > 0 && borderColor.A != 0)
        {
            context.LineWidth = borderWidth;
            context.StrokeColor = borderColor;
            backgroundRect = backgroundRect.Deflate(borderWidth / 2);
            context.StrokeRect(backgroundRect, borderRadius);
        }
    }

    /// <summary>
    /// Perform hit test on a panel.
    /// </summary>
    public static bool IsHitPanel(View view, Point point)
    {
        // Check if within bounds
        var p = view.toClient(point);
        if (!new Rect(new(0, 0), view.Size).Contains(p))
            return false;

        // Check if the background is visible (TBD: on the border and border radius)
        return view.GetProperty(Zui.Background, null).Or(Colors.TransParent).A > ALPHA_HIT_THRESHOLD;
    }

}
