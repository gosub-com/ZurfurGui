﻿namespace ZurfurGui.Controls;

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
        View.Views.Clear();
        View.Views.AddRange(_controls.Select(c => c.View));
    }

    /// <summary>
    /// Same as canvas
    /// </summary>
    public Size MeasureView(Size available)
    {
        var windowMeasured = new Size();
        foreach (var view in View.Views)
        {
            if (!view.IsVisible)
                continue;

            view.Measure(available);
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
        foreach (var view in View.Views)
            view.Arrange(new Rect(0, 0, 
                Math.Max(final.Width, view.DesiredSize.Width), Math.Max(final.Height, view.DesiredSize.Height)));

        return final;
    }

    // Forward View properties
    public bool IsVisible { get => View.IsVisible; set => View.IsVisible = value; }
    public Size Size { get => View.Size; set => View.Size = value; }
    public Size MaxSize { get => View.Size; set => View.Size = value; }
    public Size MinSize { get => View.Size; set => View.Size = value; }
    public HorizontalAlignment HorizontalAlignment { get => View.HorizontalAlignment; set => View.HorizontalAlignment = value; }
    public VerticalAlignment VerticalAlignment { get => View.VerticalAlignment; set => View.VerticalAlignment = value; }
    public Thickness Margin { get => View.Margin; set => View.Margin = value; }

}
