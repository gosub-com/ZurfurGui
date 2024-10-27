using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Render;

namespace ZurfurGui.Controls;

public class Canvas : Controllable
{
    List<Controllable> _controls = new();

    public string Type => "Canvas";
    public string Name { get; init; } = "";
    public override string ToString() => $"{Type}{(Name == "" ? "" : $":{Name}")}";
    public View View { get; private set; }

    public IList<Controllable> Controls
    {
        get => _controls;
        init => _controls = value.ToList();
    }

    public Canvas()
    {
        View = new(this);
    }

    public string Text { get; set; } = "";

    public void PopulateView()
    {
        View.Views.Clear();
        View.Views.AddRange(_controls.Select(c => c.View));
    }

    /// <summary>
    /// Same as window
    /// </summary>
    public Size MeasureView(Size available, MeasureContext measure)
    {
        var windowMeasured = new Size();
        foreach (var view in View.Views)
        {
            if (!view.IsVisible)
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

    // Forward View properties
    public bool IsVisible { get => View.IsVisible; set => View.IsVisible = value; }
    public Size Size { get => View.Size; set => View.Size = value; }
    public Size SizeMax { get => View.Size; set => View.Size = value; }
    public Size SizeMin { get => View.Size; set => View.Size = value; }
    public HorizontalAlignment AlignHorizontal { get => View.AlignHorizontal; set => View.AlignHorizontal = value; }
    public VerticalAlignment AlignVertical { get => View.AlignVertical; set => View.AlignVertical = value; }
    public Thickness Margin { get => View.Margin; set => View.Margin = value; }

}
