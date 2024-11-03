using System.Reflection;
using ZurfurGui.Render;

namespace ZurfurGui.Controls;

public class Window : Controllable
{
    public string Type => "Window";
    public string Name { get; init; } = "";
    public override string ToString() => $"{Type}{(Name == "" ? "" : $":{Name}")}";
    public View View { get; private set; }
    public Properties Properties { get; set; } = new();

    public Window()
    {
        View = new(this);
    }

    public View BuildView(Properties properties)
    {
        Properties tileCanvas = [
            (ZGui.Controller, "ZGui.Canvas"),
            (ZGui.AlignVertical, VerticalAlignment.Top),
            (ZGui.Size, new Size(double.NaN, 20)),
            (ZGui.Controls, (Properties[])[
                [
                    (ZGui.Controller, "ZGui.Button"),
                    (ZGui.AlignHorizontal, HorizontalAlignment.Left),
                    (ZGui.Text, "Menu"),
                ],
                [
                    (ZGui.Controller, "ZGui.Button"),
                    (ZGui.AlignHorizontal, HorizontalAlignment.Right),
                    (ZGui.Text, "X")
                ],
                [
                    (ZGui.Controller, "ZGui.Label"),
                    (ZGui.AlignHorizontal, HorizontalAlignment.Center),
                    (ZGui.Text, "Title")
                ]
            ])
        ];

        Properties clientCanvas = [
            (ZGui.Controller, "ZGui.Canvas"),
            (ZGui.Margin, new Thickness(0, 24, 0, 0)),
            (ZGui.Controls, properties.Getc(ZGui.Controls) ?? [])
        ];

        Properties borderCanvas = [
            (ZGui.Controller, "ZGui.Canvas"),
            (ZGui.Margin, new Thickness(5)),
            (ZGui.Controls, (Properties[])[tileCanvas, clientCanvas])
        ];


        View.Views.Clear();
        ViewHelper.AddControllers(View.Views, [borderCanvas]);
        return View;
    }


    /// <summary>
    /// Same as canvas
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
    /// A window puts all controls at (0,0), like a canvas.  Position can be controlled using margin.
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
        context.FillColor = Colors.LightGray;
        var r = new Rect(new(), View.Bounds.Size);
        context.FillRect(r);
    }
}
