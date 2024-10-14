﻿using ZurfurGui.Render;

namespace ZurfurGui.Controls;

public class Column : Controllable
{

    List<Controllable> _controls = new();

    public string Type => "Column";
    public string Name { get; init; } = "";
    public override string ToString() => $"{Type}{(Name == "" ? "" : $":{Name}")}";
    public View View { get; private set; }

    public IList<Controllable> Controls
    {
        get => _controls;
        init => _controls = value.ToList();
    }

    /// <summary>
    /// Space between elements
    /// </summary>
    public double Spacing { get; set; } = 5.0;

    public Column()
    {
        View = new(this);
    }

    public void PopulateView()
    {
        View.Views.Clear();
        View.Views.AddRange(_controls.Select(c => c.View));
    }


    public Size MeasureView(Size available, MeasureContext measure)
    {
        available.Height = double.PositiveInfinity;
        var columnMeasured = new Size();
        var visibleCount = 0;
        foreach (var view in View.Views)
        {
            if (!view.IsVisible)
                continue;

            visibleCount++;
            view.Measure(available, measure);
            var childMeasured = view.DesiredSize;
            columnMeasured.Width = Math.Max(columnMeasured.Width, childMeasured.Width);
            columnMeasured.Height += childMeasured.Height;
        }
        columnMeasured.Height += Math.Max(0, visibleCount - 1) * Spacing;
        return columnMeasured;
    }

    public Size ArrangeViews(Size final)
    {
        var bounds = new Rect(0, 0, final.Width, final.Height);
        var spacing = Spacing;
        foreach (var view in View.Views)
        {
            bounds.Height = view.DesiredSize.Height;
            bounds.Width = Math.Max(final.Width, view.DesiredSize.Width);
            view.Arrange(bounds);
            bounds.Y += view.DesiredSize.Height + spacing;
        }
        return final;
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
