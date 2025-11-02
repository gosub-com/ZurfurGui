using ZurfurGui.Base;

namespace ZurfurGui.Render;

public class DrawText : Drawable
{
    const double TEXT_BASELINE = 0.8; // Push text down so baseline is in correct place
    public const double LINE_SPACING = 1.2; // Additional space between lines
    const double CLIP_ROUNDING_ERROR = 0.0001; // Don't clip because of rounding errors

    /// <summary>
    /// Since there is no state, we can use a single instance for all text
    /// </summary>
    public static readonly DrawText Instance = new();

    public string DrawType => "Text";

    public void Draw(View view, RenderContext context)
    {
        // Quick exit when drawing outside the clip region
        if (context.DeviceClip.Intersect(view.toDevice(view.ContentRect)).Width == 0)
            return;

        var color = view.GetStyle(Zui.Color).Or(Colors.Black);
        if (color.A == 0)
            return; // Exit if clear

        // Clip if content size is larger than available size
        var contentSize = view.ContentRect.Size.Inflate(CLIP_ROUNDING_ERROR);
        if (contentSize.Width < view.DesiredContentSize.Width
            || contentSize.Height < view.DesiredContentSize.Height)
        {
            context.PushDeviceClip(view.toDevice(view.ContentRect));
        }

        var text = view.GetStyle(Zui.Text).Or(TextLines.Unknown);
        var font = view.GetStyle(Zui.Font);
        var fontName = font.Name ?? "Arial";
        var fontSize = font.Size.Or(16.0);
        context.FontName = fontName;
        context.FontSize = fontSize;
        context.FillColor = color;

        for (int i = 0; i < text.Count; i++)
            context.FillText(text[i], 0 + view.ContentRect.X,
                fontSize * TEXT_BASELINE + i * fontSize * LINE_SPACING + (LINE_SPACING - 1) * fontSize * 0.5 + view.ContentRect.Y);
    }

    public bool IsHit(View view, Point point)
    {
        var p = view.toClient(point);
        return new Rect(new(0, 0), view.DesiredTotalSize).Contains(p);
    }


}
