using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Render;

namespace ZurfurGui.Controls;

/// <summary>
///  
/// </summary>
public class Border : Controllable
{
    public string Type => "Zui.Border";
    public override string ToString() => View.ToString();
    public View View { get; private set; }

    View? _content;

    public Border()
    {
        View = new(this);
    }

    public Size MeasureView(Size available, MeasureContext measure)
    {
        var padding = View.Properties.Get(Zui.Padding);
        var borderWidth = View.Properties.Get(Zui.BorderWidth);
        return Helper.MeasureViewPanel(View, available, measure, padding + new Thickness(borderWidth));
    }

    public Size ArrangeViews(Size final, MeasureContext measure)
    {
        var padding = View.Properties.Get(Zui.Padding);
        var borderWidth = View.Properties.Get(Zui.BorderWidth);
        return Helper.ArrangeViewPanel(View, final, measure, padding + new Thickness(borderWidth));
    }

    public void Render(RenderContext context)
    {
        var r = new Rect(new(), View.Size);

        // Draw background
        var borderRadius = View.Properties.Get(Zui.BorderRadius);
        var background = View.Properties.Get(Zui.Background);
        if (background.A != 0)
        {
            context.FillColor = background;
            context.FillRect(r, borderRadius);
        }

        // Draw border
        var borderColor = View.Properties.Get(Zui.BorderColor);
        var borderWidth = View.Properties.Get(Zui.BorderWidth);
        if (borderWidth > 0 && borderColor.A != 0)
        {
            context.LineWidth = borderWidth;
            context.StrokeColor = borderColor;
            r = r.Deflate(borderWidth / 2);
            context.StrokeRect(r, borderRadius);
        }
    }

}
