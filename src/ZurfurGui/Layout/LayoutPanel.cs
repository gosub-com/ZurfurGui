using ZurfurGui.Base;
using ZurfurGui.Controls;
using ZurfurGui.Render;

namespace ZurfurGui.Layout;

public static class LayoutPanel
{

    /// <summary>
    /// Measure a view according to the rules of a panel.
    /// </summary>
    public static Size MeasurePanel(this View view, MeasureContext measure, Size available)
    {
        var windowMeasured = new Size();
        foreach (var child in view.Children)
        {
            var viewIsVisible = child.IsVisible;
            if (!viewIsVisible)
                continue;

            child.Measure(available, measure);
            var childMeasured = child.DesiredTotalSize;
            var offset = child.GetProperty(Panel.Offset).Or(0);
            windowMeasured.Width = Math.Max(windowMeasured.Width, Math.Max(0, offset.X + childMeasured.Width));
            windowMeasured.Height = Math.Max(windowMeasured.Height, Math.Max(0, offset.Y + childMeasured.Height));
        }

        return windowMeasured;
    }

    /// <summary>
    /// Arrange a view according to the rules of a panel.
    /// </summary>
    public static void ArrangePanel(this View view, MeasureContext measure)
    {
        // All children get positioned at absolute coordinates in the contentRect
        var contentRect = view.ContentRect;
        foreach (var child in view.Children)
            if (child.IsVisible)
            {
                var offset = child.GetProperty(Panel.Offset).Or(0);
                child.Arrange(
                    new Rect(
                        contentRect.X + offset.X,
                        contentRect.Y + offset.Y,
                        contentRect.Width,
                        contentRect.Height),
                    measure);
            }
    }

}
