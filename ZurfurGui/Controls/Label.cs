
using System.Reflection.Emit;
using ZurfurGui.Platform;
using ZurfurGui.Render;

namespace ZurfurGui.Controls;

public class Label : Controllable
{
    public static readonly PropertyIndex<string> FontNamePi = new("FontName");
    public static readonly PropertyIndex<double> FontSizePi = new("FontSize");

    // Expand lines over the size of the font itself
    const double LINE_SPACING = 1;

    public string Type => "Label";
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

    public Label()
    {
        View = new(this);
    }

    public void PopulateView()
    {
    }

    public Size MeasureView(Size available, MeasureContext measure)
    {
        var fontName = Properties.Getc(FontNamePi) ?? FontName;
        var fontSize = Properties.Gets(FontSizePi) ?? FontSize;
        var text = Properties.Getc(View.Text) ?? Text;
        var lines = text.Split("\n");
        var maxWidth = lines.Max(line => measure.MeasureTextWidth(fontName, fontSize, line));
        return new Size(maxWidth, lines.Length * LINE_SPACING * fontSize);
    }
    public Size ArrangeViews(Size final) => final;


    public void Render(RenderContext context)
    {
        var fontName = Properties.Getc(FontNamePi) ?? FontName;
        var fontSize = Properties.Gets(FontSizePi) ?? FontSize;
        var text = Properties.Getc(View.Text) ?? Text;
        context.FontName = fontName;
        context.FontSize = fontSize;
        context.FillColor = Colors.White;
        context.FillText(text, 0, fontSize);

        context.LineWidth = 1;
        context.StrokeColor = Colors.Gray;
        context.StrokeRect(0, 0, View.Bounds.Width, View.Bounds.Height);
    }
}
