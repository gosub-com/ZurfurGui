using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Base;

namespace ZurfurGui.Draw;
public static class DrawHelper
{
    const byte ALPHA_HIT_THRESHOLD = 16; // Must be visible to user before it's a hit

    /// <summary>
    /// Draws a panel with a border & background.
    /// </summary>
    public static void DrawBackground(View view, RenderContext context)
    {
        // Quick exit when drawing outside the clip region
        if (context.DeviceClip.Intersect(new Rect(view.Origin, view.toDevice(view.Size))).Width == 0)
            return;

        // Draw background
        var back = view.GetStyle(Zui.Background);
        var borderRadius = back.Radius.Or(0);
        var background = back.Color.Or(Colors.TransParent);
        if (background.A != 0)
        {
            context.FillColor = background;
            context.FillRect(new Rect(new(), view.Size), borderRadius);
        }

        // Draw border
        var borderColor = back.BorderColor.Or(Colors.TransParent);
        var borderWidth = back.BorderWidth.Or(0);
        if (borderWidth > 0 && borderColor.A != 0)
        {
            context.LineWidth = borderWidth;
            context.StrokeColor = borderColor;
            context.StrokeRect(new Rect(new(), view.Size).Deflate(borderWidth / 2), borderRadius);
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
        return view.GetStyle(Zui.Background).Color.Or(Colors.TransParent).A > ALPHA_HIT_THRESHOLD;
    }

}
