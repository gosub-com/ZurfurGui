using ZurfurGui.Base;
using ZurfurGui.Controls;

namespace ZurfurGui.Render;
internal static class RenderHelper
{
    const byte ALPHA_HIT_THRESHOLD = 16; // Must be visible to user before it's a hit

    /// <summary>
    /// Renders a panel with a border & background.
    /// </summary>
    public static void RenderBackground(View view, RenderContext context)
    {
        // Render background
        var borderRadius = view.GetStyle(Panel.BorderRadius);
        var background = view.GetStyle(Panel.BackgroundColor);
        var borderWidth = Math.Min(view.GetStyle(Panel.BorderWidth), Math.Min(view.Size.Width, view.Size.Height)/2);
        if (background.A != 0)
        {
            context.FillRect(background, new Rect(new(), view.Size).Deflate(borderWidth/2), borderRadius);
        }

        // Render border
        var borderColor = view.GetStyle(Panel.BorderColor);
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
