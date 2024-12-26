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
    static readonly int BORDER_WIDTH = 2;
    static readonly int PADDING = 5;

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
        return Helper.MeasureView(View, available, measure, new(BORDER_WIDTH + PADDING));
    }

    public Size ArrangeViews(Size final, MeasureContext measure)
    {
        return Helper.ArrangeView(View, final, measure, new(BORDER_WIDTH + PADDING));
    }

    public void Render(RenderContext context)
    {
        var r = new Rect(new(), View.Size);

        context.FillColor = Colors.Blue;
        context.FillRect(r);


        context.LineWidth = BORDER_WIDTH;
        context.StrokeColor = Colors.Yellow;
        r = r.Deflate(BORDER_WIDTH/2);
        context.StrokeRect(r);
    }

}
