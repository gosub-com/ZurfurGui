
using System.Reflection.Emit;
using ZurfurGui.Platform;
using ZurfurGui.Render;

namespace ZurfurGui.Controls;

public class Label : Controllable
{
    // Expand lines over the size of the font itself
    const double LINE_SPACING = 1;
    const double TEXT_BASELINE = 0.8;

    public string Type => "ZGui.Label";
    public override string ToString() => $"{View.Properties.Get(ZGui.Id) ?? ""}:{Type}";
    public View View { get; private set; }

    public Label()
    {
        View = new(this);
    }

    public View BuildView(Properties properties)
    {
        return View;
    }

    public Size MeasureView(Size available, MeasureContext measure)
    {
        var fontName = View.Properties.Get(ZGui.FontName) ?? "Arial";
        var fontSize = View.Properties.Get(ZGui.FontSize, 16.0);
        var text = View.Properties.Get(ZGui.Text) ?? "";
        var lines = text.Split("\n");
        var maxWidth = lines.Max(line => measure.MeasureTextWidth(fontName, fontSize, line));
        return new Size(maxWidth, lines.Length * LINE_SPACING * fontSize);
    }
    public Size ArrangeViews(Size final, MeasureContext measure) => final;

    public void Render(RenderContext context)
    {
        var fontName = View.Properties.Get(ZGui.FontName) ?? "Arial";
        var fontSize = View.Properties.Get(ZGui.FontSize, 16.0);
        var text = View.Properties.Get(ZGui.Text) ?? "";
        context.FontName = fontName;
        context.FontSize = fontSize;
        context.FillColor = Colors.White;

        // Mouse over color change
        if (View.toDevice(View.Clip).Contains(context.PointerPosition))
            context.FillColor = Colors.Red;


        context.FillText(text, 0, fontSize*TEXT_BASELINE);

        context.LineWidth = 1;
        context.StrokeColor = Colors.Gray;
        context.StrokeRect(0, 0, View.Bounds.Width, View.Bounds.Height);
    }
}
