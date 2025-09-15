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
            var viewIsVisible = childView.GetStyle(Zui.IsVisible, true);
            if (!viewIsVisible)
                continue;

            visibleCount++;
            childView.Measure(available, measure);
            var childMeasured = childView.DesiredSize;
            columnMeasured.Width = Math.Max(columnMeasured.Width, childMeasured.Width);
            columnMeasured.Height += childMeasured.Height;
        }
        columnMeasured.Height += Math.Max(0, visibleCount - 1) * Spacing;
        return columnMeasured;
    }

    public Size ArrangeViews(View view, MeasureContext measure, Size final, Rect contentRect)
    {
        var bounds = contentRect;
        var spacing = Spacing;
        foreach (var childView in view.Children)
        {
            bounds.Height = childView.DesiredSize.Height;
            bounds.Width = Math.Max(contentRect.Width, childView.DesiredSize.Width);
            childView.Arrange(bounds, measure);
            bounds.Y += childView.DesiredSize.Height + spacing;
        }
        return final;
    }
}
