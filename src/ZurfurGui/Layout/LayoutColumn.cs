using ZurfurGui.Base;
using ZurfurGui.Controls;
using ZurfurGui.Render;

namespace ZurfurGui.Layout;

/// <summary>
/// Layout columns.  TBD: This will be replaced with LayoutRow, so no new devlopment here.
/// </summary>
public class LayoutColumn : Layoutable
{

    /// <summary>
    /// Default space between elements, used when the view has no <c>cell.spacing</c> style.
    /// </summary>
    public double Spacing { get; set; } = 5.0;

    string Layoutable.TypeName => "Column";


    Size Layoutable.MeasureView(View view, MeasureContext measure, Size available)
    {
        var columnMeasured = new Size();
        var visibleCount = 0;
        foreach (var childView in view.Children)
        {
            var viewIsVisible = childView.IsVisible;
            if (!viewIsVisible)
                continue;

            visibleCount++;
            childView.Measure(available, measure);
            var childMeasured = childView.DesiredTotalSize;
            columnMeasured.Width = Math.Max(columnMeasured.Width, childMeasured.Width);
            columnMeasured.Height += childMeasured.Height;
        }
        columnMeasured.Height += Math.Max(0, visibleCount - 1) * GetSpacing(view);
        return columnMeasured;
    }

    void Layoutable.ArrangeViews(View view, MeasureContext measure)
    {
        var bounds = view.ContentRect;
        var spacing = GetSpacing(view);
        foreach (var childView in view.Children)
        {
            bounds.Height = childView.DesiredTotalSize.Height;
            bounds.Width = Math.Max(view.ContentRect.Width, childView.DesiredTotalSize.Width);
            childView.Arrange(bounds, measure);
            bounds.Y += childView.DesiredTotalSize.Height + spacing;
        }
    }

    /// <summary>
    /// Vertical space between children.  The <c>cell.spacing</c> style (the same one LayoutRow
    /// uses, so a theme can set it) wins, otherwise fall back to <see cref="Spacing"/>.
    /// </summary>
    double GetSpacing(View view)
    {
        return view.GetProperty(LayoutRow.Spacing).Height.Or(Spacing);
    }
}
