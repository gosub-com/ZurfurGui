using ZurfurGui.Base;
using ZurfurGui.Controls;

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
    public bool PromiseToDrawInsideControl => true;

    public void Draw(View view, RenderContext context)
    {
        var color = view.GetStyle(TextView.ColorProperty);
        if (color.A == 0)
            return; // Exit if clear

        var text = view.GetStyle(TextView.TextProperty);

        // Clip if content size is larger than available size
        var contentSize = view.ContentRect.Size.Inflate(CLIP_ROUNDING_ERROR);
        if (contentSize.Width < view.DesiredContentSize.Width
            || contentSize.Height < view.DesiredContentSize.Height)
        {
            context.PushClip(view.ContentRect);
        }

        var fontProp = view.GetStyle(TextView.FontProperty);
        var fontName = fontProp.Name ?? "Arial";
        var fontSize = fontProp.Size.Or(16.0);
        var font = new Font(fontName, fontSize);
        var brush = new Brush(color);

        for (int i = 0; i < text.Count; i++)
            context.FillText(font, brush, text[i], 0 + view.ContentRect.X,
                fontSize * TEXT_BASELINE + i * fontSize * LINE_SPACING + (LINE_SPACING - 1) * fontSize * 0.5 + view.ContentRect.Y);    
    }

    public bool IsHit(View view, Point point)
    {
        var p = view.toClient(point);
        return new Rect(new(0, 0), view.DesiredTotalSize).Contains(p);
    }


}
