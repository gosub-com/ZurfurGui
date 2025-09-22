using System.Reflection.Metadata;
using ZurfurGui.Base;
using ZurfurGui.Controls;

namespace ZurfurGui.Layout;

public class LayoutColumn : Layoutable
{

    /// <summary>
    /// Space between elements
    /// </summary>
    public double Spacing { get; set; } = 5.0;

    public string TypeName => "Column";


    public Size MeasureView(View view, MeasureContext measure, Size available)
    {
        var columnMeasured = new Size();
        var visibleCount = 0;
        foreach (var childView in view.Children)
        {
            var viewIsVisible = childView.GetStyle(Zui.IsVisible);
            if (!viewIsVisible)
                continue;

            visibleCount++;
            childView.Measure(available, measure);
            var childMeasured = childView.DesiredTotalSize;
            columnMeasured.Width = Math.Max(columnMeasured.Width, childMeasured.Width);
            columnMeasured.Height += childMeasured.Height;
        }
        columnMeasured.Height += Math.Max(0, visibleCount - 1) * Spacing;
        return columnMeasured;
    }

    public void ArrangeViews(View view, MeasureContext measure)
    {
        var bounds = view.ContentRect;
        var spacing = Spacing;
        foreach (var childView in view.Children)
        {
            bounds.Height = childView.DesiredTotalSize.Height;
            bounds.Width = Math.Max(view.ContentRect.Width, childView.DesiredTotalSize.Width);
            childView.Arrange(bounds, measure);
            bounds.Y += childView.DesiredTotalSize.Height + spacing;
        }
    }
}
