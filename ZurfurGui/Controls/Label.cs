
using System.Reflection.Emit;
using ZurfurGui.Platform;
using ZurfurGui.Render;

namespace ZurfurGui.Controls;

public partial class Label : Controllable
{

    public Label()
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
        var color = View.PointerHoverTarget ? Colors.Red : Colors.White;
        Helper.RenderText(View, context, color);
    }


    public bool IsHit(Point point)
    {
        var p = View.toClient(point);
        return new Rect(new(0, 0), View.DesiredSize).Contains(p);
    }

}
