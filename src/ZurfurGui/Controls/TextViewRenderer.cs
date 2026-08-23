using ZurfurGui.Base;
using ZurfurGui.Render;

namespace ZurfurGui.Controls;

internal class TextViewRenderer : Renderable
{
    const double CLIP_ROUNDING_ERROR = 0.0001; // Don't clip because of rounding errors

    /// <summary>
    /// Since there is no state, we can use a single instance for all text
    /// </summary>
    public static readonly TextViewRenderer Instance = new();

    public string RenderType => "Text";

    public void Render(View view, RenderContext context)
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
        var metrics = context.MeasureContext.GetFontMetrics(fontName, fontSize);

        // First line baseline is positioned at ContentRect.Y + Ascent
        var baselineY = view.ContentRect.Y + metrics.Ascent;

        for (int i = 0; i < text.Count; i++)
        {
            var y = baselineY + i * metrics.LineHeight;
            context.FillText(font, brush, text[i], view.ContentRect.X, y);
        }
    }

    public bool IsHit(View view, Point point)
    {
        var p = view.toClient(point);
        return new Rect(new(0, 0), view.DesiredTotalSize).Contains(p);
    }


}
