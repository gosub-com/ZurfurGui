using ZurfurGui.Base;

namespace ZurfurGui.Render;
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
        var borderRadius = view._cache.BorderRadius;
        var background = view._cache.BackgroundColor;
        var borderWidth = Math.Min(view._cache.BorderWidth, Math.Min(view.Size.Width, view.Size.Height)/2);
        if (background.A != 0)
        {
            context.FillColor = background;
            context.FillRect(new Rect(new(), view.Size).Deflate(borderWidth/2), borderRadius);
        }

        // Draw border
        var borderColor = view._cache.BorderColor;
        if (borderWidth > 0 && borderColor.A != 0)
        {
            context.LineWidth = borderWidth;
            context.StrokeColor = borderColor;
            context.StrokeRect(new Rect(new(), view.Size).Deflate(borderWidth/2), borderRadius);
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
        return view.GetStyle(Zui.BackgroundColor).Or(Colors.TransParent).A > ALPHA_HIT_THRESHOLD;
    }

}
