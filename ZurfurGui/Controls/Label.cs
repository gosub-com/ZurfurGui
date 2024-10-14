
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

    /// <summary>
    /// Dummy measurement until we get fonts (20 pixels per character for now)
    /// </summary>
    public Size MeasureView(Size available, MeasureContext measure)
    {
        var lines = Text.Split("\n");
        var maxWidth = lines.Max(line => measure.MeasureTextWidth(FontName, FontSize, line));
        return new Size(maxWidth, lines.Length * LINE_SPACING * FontSize);
    }
    public Size ArrangeViews(Size final) => final;


    public void Render(RenderContext context)
    {
        context.FontName = FontName;
        context.FontSize = FontSize;
        context.FillColor = Colors.White;
        context.FillText(Text, 0, FontSize);
    }


    // Forward View properties
    public bool IsVisible { get => View.IsVisible; set => View.IsVisible = value; }
    public Size Size { get => View.Size; set => View.Size = value; }
    public Size SizeMax { get => View.Size; set => View.Size = value; }
    public Size SizeMin { get => View.Size; set => View.Size = value; }
    public HorizontalAlignment AlignHorizontal { get => View.AlignHorizontal; set => View.AlignHorizontal = value; }
    public VerticalAlignment AlignVertical { get => View.AlignVertical; set => View.AlignVertical = value; }
    public Thickness Margin { get => View.Margin; set => View.Margin = value; }
}
