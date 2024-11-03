using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Render;

namespace ZurfurGui.Controls;

public class Canvas : Controllable
{
    public string Type => "Canvas";
    public string Name { get; init; } = "";
    public override string ToString() => $"{Type}{(Name == "" ? "" : $":{Name}")}";
    public View View { get; private set; }
    public Properties Properties { get; set; } = new();

    public Canvas()
    {
        View = new(this);
    }

    public View BuildView(Properties properties)
    {
        View.Views.Clear();
        ViewHelper.AddControllers(View.Views, properties.Getc(ZGui.Controls));
        return View;
    }

    /// <summary>
    /// Same as window
    /// </summary>
    public Size MeasureView(Size available, MeasureContext measure)
    {
        var windowMeasured = new Size();
        foreach (var view in View.Views)
        {
            var viewIsVisible = view.Control?.Properties.Gets(ZGui.IsVisible) ?? true;
            if (!viewIsVisible)
                continue;

            view.Measure(available, measure);
            var childMeasured = view.DesiredSize;
            windowMeasured.Width = Math.Max(windowMeasured.Width, childMeasured.Width);
            windowMeasured.Height = Math.Max(windowMeasured.Height, childMeasured.Height);
        }
        return windowMeasured;
    }

    /// <summary>
    /// A canvas puts all controls at (0,0), like a window.  Position can be controlled using margin.
    /// </summary>
    public Size ArrangeViews(Size final)
    {
        var layoutRect = new Rect(0, 0, final.Width, final.Height);
        foreach (var view in View.Views)
            view.Arrange(layoutRect);

        return final;
    }

    public void Render(RenderContext context)
    {
        var BORDER_WIDTH = 2;
        var r = new Rect(new(), View.Bounds.Size);
        context.LineWidth = BORDER_WIDTH;
        context.StrokeColor = Colors.Yellow;
        r = r.Deflate(BORDER_WIDTH/2);
        context.StrokeRect(r);
    }

}
