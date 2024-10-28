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
    public Properties Properties { get; init; } = new();

    public IList<Controllable> Controls
    {
        get => Array.Empty<Controllable>();
        init => throw new NotImplementedException("Controls not allowed");
    }

    public string Text { get; set; } = "";
    public string FontName { get; set; } = "Arial";
    public double FontSize { get; set; } = 16;

    public Button()
    {
        View = new(this);
    }

    public void PopulateView()
    {
    }

    public Size MeasureView(Size available, MeasureContext measure)
    {
        var text = Properties.Getc(View.Text) ?? Text;
        var lines = text.Split("\n");
        var maxWidth = lines.Max(line => measure.MeasureTextWidth(FontName, FontSize, line));
        return new Size(maxWidth, lines.Length * LINE_SPACING * FontSize);
    }
    public Size ArrangeViews(Size final) => final;


    public void Render(RenderContext context)
    {
        var text = Properties.Getc(View.Text) ?? Text;
        context.FillColor = Colors.Gray;
        context.FillRect(0, 0, View.Bounds.Width, View.Bounds.Height);
        context.FontName = FontName;
        context.FontSize = FontSize;
        context.FillColor = Colors.White;
        context.FillText(text, 0, FontSize);
    }
}
