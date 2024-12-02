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
    const double TEXT_BASELINE = 0.8;

    public string Type => "ZGui.Button";
    public override string ToString() => View.ToString();
    public View View { get; private set; }

    public Button()
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
        context.FillColor = View.PointerHoverTarget ? Colors.Red : Colors.Gray;
        context.FillRect(0, 0, View.Size.Width, View.Size.Height);

        var color = Colors.White;
        var text = View.Properties.Get(ZGui.Text) ?? "";
        var fontName = View.Properties.Get(ZGui.FontName) ?? "Arial";
        var fontSize = View.Properties.Get(ZGui.FontSize, 16.0);
        context.FontName = fontName;
        context.FontSize = fontSize;
        context.FillColor = color;
        context.FillText(text, 0, fontSize * TEXT_BASELINE);
    }

    public bool IsHit(Point point)
    {
        var p = View.toClient(point);
        return new Rect(new(0,0), View.Size).Contains(p);
    }
}
