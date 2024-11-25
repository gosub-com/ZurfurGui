using ZurfurGui.Render;

namespace ZurfurGui.Controls;

public class Column : Controllable
{
    public string Type => "ZGui.Column";
    public override string ToString() => View.ToString();
    public View View { get; private set; }

    /// <summary>
    /// Space between elements
    /// </summary>
    public double Spacing { get; set; } = 5.0;

    public Column()
    {
        View = new(this);
    }

    public View BuildView(Properties properties)
    {
        View.Views.Clear();
        Helper.BuildViewsFromProperties(View.Views, properties.Get(ZGui.Controls));
        return View;
    }

    public Size MeasureView(Size available, MeasureContext measure)
    {
        available.Height = double.PositiveInfinity;
        var columnMeasured = new Size();
        var visibleCount = 0;
        foreach (var view in View.Views)
        {
            var viewIsVisible = view.Properties.Get(ZGui.IsVisible, true);
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

    public Size ArrangeViews(Size final, MeasureContext measure)
    {
        var bounds = new Rect(0, 0, final.Width, final.Height);
        var spacing = Spacing;
        foreach (var view in View.Views)
        {
            bounds.Height = view.DesiredSize.Height;
            bounds.Width = Math.Max(final.Width, view.DesiredSize.Width);
            view.Arrange(bounds, measure);
            bounds.Y += view.DesiredSize.Height + spacing;
        }
        return final;
    }
}
