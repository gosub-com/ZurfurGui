using ZurfurGui.Base;
using ZurfurGui.Controls;

namespace ZurfurGui.Render;
public static class DrawHelper
{
    const byte ALPHA_HIT_THRESHOLD = 16; // Must be visible to user before it's a hit

    /// <summary>
    /// Draws a panel with a border & background.
    /// </summary>
    public static void DrawBackground(View view, RenderContext context)
    {
        // Draw background
        var borderRadius = view._measureCache.BorderRadius;
        var background = view._measureCache.BackgroundColor;
        var borderWidth = Math.Min(view._measureCache.BorderWidth, Math.Min(view.Size.Width, view.Size.Height)/2);
        if (background.A != 0)
        {
            context.FillRect(background, new Rect(new(), view.Size).Deflate(borderWidth/2), borderRadius);
        }

        // Draw border
        var borderColor = view._measureCache.BorderColor;
        if (borderWidth > 0 && borderColor.A != 0)
        {
            context.StrokeRect(new Pen(borderColor, borderWidth), new Rect(new(), view.Size).Deflate(borderWidth/2), borderRadius);
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
        return view.GetStyle(Panel.BackgroundColor).A > ALPHA_HIT_THRESHOLD;
    }

}
