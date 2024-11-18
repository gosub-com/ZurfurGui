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
    const double TEXT_BASELINE = 0.8;

    public string Type => "ZGui.Button";
    public override string ToString() => $"{View.Properties.Get(ZGui.Id) ?? ""}:{Type}";
    public View View { get; private set; }

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
        var text = View.Properties.Get(ZGui.Text) ?? "";
        var fontName = View.Properties.Get(ZGui.FontName) ?? "Arial";
        var fontSize = View.Properties.Get(ZGui.FontSize, 16);

        var lines = text.Split("\n");
        var maxWidth = lines.Max(line => measure.MeasureTextWidth(fontName, fontSize, line));
        return new Size(maxWidth, lines.Length * LINE_SPACING * fontSize);
    }
    public Size ArrangeViews(Size final, MeasureContext measure) => final;


    public void Render(RenderContext context)
    {
        var text = View.Properties.Get(ZGui.Text) ?? "";
        var fontName = View.Properties.Get(ZGui.FontName) ?? "Arial";
        var fontSize = View.Properties.Get(ZGui.FontSize, 16);

        context.FillColor = Colors.Gray;

        // Mouse over color change
        if (new Rect(View.Origin + View.Clip.Position, View.Clip.Size * View.Scale).Contains(context.PointerPosition))
            context.FillColor = Colors.Red;

        context.FillRect(0, 0, View.Bounds.Width, View.Bounds.Height);
        context.FontName = fontName;
        context.FontSize = fontSize;
        context.FillColor = Colors.White;
        context.FillText(text, 0, fontSize * TEXT_BASELINE);
    }
}
