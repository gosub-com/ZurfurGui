using ZurfurGui.Render;

namespace ZurfurGui.Controls;

public class Column : Controllable
{

    List<Controllable> _controls = new();

    public string Type => "Column";
    public string Name { get; init; } = "";
    public override string ToString() => $"{Type}{(Name == "" ? "" : $":{Name}")}";
    public View View { get; private set; }
    public Properties Properties { get; init; } = new();

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
            var viewIsVisible = view.Control?.Properties.Gets(View.IsVisiblePi) ?? true;
            if (!viewIsVisible)
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
}
