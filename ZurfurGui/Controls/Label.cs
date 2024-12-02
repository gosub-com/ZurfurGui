
using System.Reflection.Emit;
using ZurfurGui.Platform;
using ZurfurGui.Render;

namespace ZurfurGui.Controls;

public class Label : Controllable
{
    // Expand lines over the size of the font itself
    const double TEXT_BASELINE = 0.8;

    public string Type => "ZGui.Label";
    public override string ToString() => View.ToString();
    public View View { get; private set; }

    public Label()
    {
        View = new(this);
    }

    public void Build()
    {
    }

    public Size MeasureView(Size available, MeasureContext measure)
    {
        return Helper.MeasureText(measure, View);
    }

    public Size ArrangeViews(Size final, MeasureContext measure) => final;

    public void Render(RenderContext context)
    {
        // Draw background
        context.FillColor = Colors.Purple;
        context.FillRect(0,0, View.Size.Width, View.Size.Height);

        var color = View.PointerHoverTarget ? Colors.Red : Colors.White;
        var text = View.Properties.Get(ZGui.Text) ?? "";
        var fontName = View.Properties.Get(ZGui.FontName) ?? "Arial";
        var fontSize = View.Properties.Get(ZGui.FontSize, 16.0);
        context.FontName = fontName;
        context.FontSize = fontSize;
        context.FillColor = color;
        context.FillText(text, 0, fontSize*TEXT_BASELINE);

        // TBD: Single line border is partially hidden on Windows
        // context.LineWidth = 1;
        // context.StrokeColor = Colors.Gray;
        // context.StrokeRect(0, 0, View.Size.Width, View.Size.Height);
    }

    public bool IsHit(Point point)
    {
        var p = View.toClient(point);
        return new Rect(new(0, 0), View.DesiredSize).Contains(p);
    }

}
