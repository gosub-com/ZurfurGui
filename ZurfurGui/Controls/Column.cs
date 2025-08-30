using ZurfurGui.Render;

namespace ZurfurGui.Controls;

public partial class Column : Controllable
{

    /// <summary>
    /// Space between elements
    /// </summary>
    public double Spacing { get; set; } = 5.0;

    public Column()
    {
        InitializeComponent();
    }
    public void LoadContent()
    {
        Loader.BuildViews(View, View.Properties.Get(Zui.Content));
    }

    public Size MeasureView(Size available, MeasureContext measure)
    {
        available.Height = double.PositiveInfinity;
        var columnMeasured = new Size();
        var visibleCount = 0;
        foreach (var view in View.Views)
        {
            var viewIsVisible = view.Properties.Get(Zui.IsVisible, true);
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
