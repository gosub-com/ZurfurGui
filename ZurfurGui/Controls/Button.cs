using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Render;

namespace ZurfurGui.Controls;

public partial class Button : Controllable
{

    public Button()
    {
        InitializeComponent();
    }

    public Size MeasureView(Size available, MeasureContext measure)
    {
        return Helper.MeasureText(measure, View);
    }

    public void Render(RenderContext context)
    {
        // Draw background
        context.FillColor = View.PointerHoverTarget ? Colors.Red : Colors.Gray;
        context.FillRect(0, 0, View.Size.Width, View.Size.Height);
        var color = Colors.White;

        Helper.RenderText(View, context, color);
    }

    public bool IsHit(Point point)
    {
        var p = View.toClient(point);
        return new Rect(new(0,0), View.Size).Contains(p);
    }
}
