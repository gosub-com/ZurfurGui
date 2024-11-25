using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Render;

namespace ZurfurGui.Controls;

public class Panel : Controllable
{
    public string Type => "ZGui.Panel";
    public override string ToString() => View.ToString();
    public View View { get; private set; }

    public Panel()
    {
        View = new(this);
    }

    public View BuildView(Properties properties)
    {
        View.Views.Clear();
        Helper.BuildViewsFromProperties(View.Views, properties.Get(ZGui.Controls));
        return View;
    }

    /// <summary>
    /// Same as window
    /// </summary>
    public Size MeasureView(Size available, MeasureContext measure)
    {
        return Helper.MeasurePanel(View.Views, available, measure);
    }

    /// <summary>
    /// A panel puts all controls at (0,0), like a window.  Position can be controlled using margin.
    /// </summary>
    public Size ArrangeViews(Size final, MeasureContext measure)
    {
        return Helper.ArrangePanel(View.Views, final, measure);
    }

    public void Render(RenderContext context)
    {
        var BORDER_WIDTH = 2;
        var r = new Rect(new(), View.Size);
        context.LineWidth = BORDER_WIDTH;
        context.StrokeColor = Colors.Yellow;
        r = r.Deflate(BORDER_WIDTH/2);
        context.StrokeRect(r);
    }

}
