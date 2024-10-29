using System.Reflection;
using ZurfurGui.Render;

namespace ZurfurGui.Controls;

public class Window : Controllable
{
    List<Controllable> _controls = new();

    public string Type => "Window";
    public string Name { get; init; } = "";
    public override string ToString() => $"{Type}{(Name == "" ? "" : $":{Name}")}";
    public View View { get; private set; }
    public Properties Properties { get; init; } = new();

    public IList<Controllable> Controls
    {
        get => _controls;
        init => _controls = value.ToList();
    }

    public Window()
    {
        View = new(this);
    }

    public string Text { get; set; } = "";

    public void PopulateView()
    {
        var tileCanvas = new Canvas()
        {
            Properties = [
                (View.AlignVertical, VerticalAlignment.Top),
                (View.Size, new Size(double.NaN, 20)),
            ],

            Controls = [
                new Button() {
                    Properties = [
                        (View.AlignHorizontal, HorizontalAlignment.Left),
                        (View.Text, "Menu")
                    ]
                },
                new Button() {
                    Properties = [
                        (View.AlignHorizontal, HorizontalAlignment.Right), 
                        (View.Text, "X")
                    ]
                },
                new Label() {
                    Properties = [
                        (View.AlignHorizontal, HorizontalAlignment.Center),
                        (View.Text, "Title")
                    ]
                },
            ]
        };

        var clientCanvas = new Canvas()
        {
            Controls = _controls,
            Properties = [
                (View.Margin, new Thickness(0, 24, 0, 0)),
            ]
        };

        var borderCanvas = new Canvas() { 
            Controls = [tileCanvas,clientCanvas],
            Properties = [
                (View.Margin, new Thickness(5)),
            ]
        };

        View.Views.Clear();
        View.Views.Add(borderCanvas.View);
    }

    /// <summary>
    /// Same as canvas
    /// </summary>
    public Size MeasureView(Size available, MeasureContext measure)
    {
        var windowMeasured = new Size();
        foreach (var view in View.Views)
        {
            var viewIsVisible = view.Control?.Properties.Gets(View.IsVisible) ?? true;
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
