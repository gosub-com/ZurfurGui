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
            Margin = new(0),
            Size = new(double.NaN, 20),
            AlignVertical = VerticalAlignment.Top,
            Controls = [
                new Button() {
                    Text = "Menu",
                    AlignHorizontal = HorizontalAlignment.Left,
                },
                new Button() {
                    Text = "X",
                    AlignHorizontal = HorizontalAlignment.Right,
                },
                new Label() {
                    Text = "Title",
                    AlignHorizontal = HorizontalAlignment.Center,
                },
            ]
        };

        var clientCanvas = new Canvas()
        {
            Margin = new(0, 24, 0, 0),
            Controls = _controls,
        };

        var borderCanvas = new Canvas() { 
            Margin = new(5),
            Controls = [tileCanvas,clientCanvas]
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

    // Forward View properties
    public bool IsVisible { get => View.IsVisible; set => View.IsVisible = value; }
    public Size Size { get => View.Size; set => View.Size = value; }
    public Size SizeMax { get => View.Size; set => View.Size = value; }
    public Size SizeMin { get => View.Size; set => View.Size = value; }
    public HorizontalAlignment AlignHorizontal { get => View.AlignHorizontal; set => View.AlignHorizontal = value; }
    public VerticalAlignment AlignVertical { get => View.AlignVertical; set => View.AlignVertical = value; }
    public Thickness Margin { get => View.Margin; set => View.Margin = value; }

}
