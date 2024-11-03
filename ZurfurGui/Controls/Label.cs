
using System.Reflection.Emit;
using ZurfurGui.Platform;
using ZurfurGui.Render;

namespace ZurfurGui.Controls;

public class Label : Controllable
{
    // Expand lines over the size of the font itself
    const double LINE_SPACING = 1;

    public string Type => "Label";
    public string Name { get; init; } = "";
    public override string ToString() => $"{Type}{(Name == "" ? "" : $":{Name}")}";
    public View View { get; private set; }
    public Properties Properties { get; set; } = new();

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
        var fontName = Properties.Getc(ZGui.FontName) ?? "Arial";
        var fontSize = Properties.Gets(ZGui.FontSize) ?? 16.0;
        var text = Properties.Getc(ZGui.Text) ?? "";
        var lines = text.Split("\n");
        var maxWidth = lines.Max(line => measure.MeasureTextWidth(fontName, fontSize, line));
        return new Size(maxWidth, lines.Length * LINE_SPACING * fontSize);
    }
    public Size ArrangeViews(Size final) => final;


    public void Render(RenderContext context)
    {
        var fontName = Properties.Getc(ZGui.FontName) ?? "Arial";
        var fontSize = Properties.Gets(ZGui.FontSize) ?? 16.0;
        var text = Properties.Getc(ZGui.Text) ?? "";
        context.FontName = fontName;
        context.FontSize = fontSize;
        context.FillColor = Colors.White;
        context.FillText(text, 0, fontSize);

        context.LineWidth = 1;
        context.StrokeColor = Colors.Gray;
        context.StrokeRect(0, 0, View.Bounds.Width, View.Bounds.Height);
    }
}
