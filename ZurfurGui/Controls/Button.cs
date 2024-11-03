using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Render;

namespace ZurfurGui.Controls;

public class Button : Controllable
{
    // Expand lines over the size of the font itself
    const double LINE_SPACING = 1;

    public string Type => "Button";
    public string Name { get; init; } = "";
    public override string ToString() => $"{Type}{(Name == "" ? "" : $":{Name}")}";
    public View View { get; private set; }
    public Properties Properties { get; set; } = new();

    public Button()
    {
        View = new(this);
    }

    public View BuildView(Properties properties)
    {
        return View;
    }

    public Size MeasureView(Size available, MeasureContext measure)
    {
        var text = Properties.Getc(ZGui.Text) ?? "";
        var fontName = Properties.Getc(ZGui.FontName) ?? "Arial";
        var fontSize = Properties.Gets(ZGui.FontSize) ?? 16;

        var lines = text.Split("\n");
        var maxWidth = lines.Max(line => measure.MeasureTextWidth(fontName, fontSize, line));
        return new Size(maxWidth, lines.Length * LINE_SPACING * fontSize);
    }
    public Size ArrangeViews(Size final) => final;


    public void Render(RenderContext context)
    {
        var text = Properties.Getc(ZGui.Text) ?? "";
        var fontName = Properties.Getc(ZGui.FontName) ?? "Arial";
        var fontSize = Properties.Gets(ZGui.FontSize) ?? 16;

        context.FillColor = Colors.Gray;
        context.FillRect(0, 0, View.Bounds.Width, View.Bounds.Height);
        context.FontName = fontName;
        context.FontSize = fontSize;
        context.FillColor = Colors.White;
        context.FillText(text, 0, fontSize);
    }
}
