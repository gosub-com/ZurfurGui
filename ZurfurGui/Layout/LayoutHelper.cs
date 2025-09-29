using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Base;
using static ZurfurGui.Base.Size;

namespace ZurfurGui.Layout;

public static class LayoutHelper
{

    /// <summary>
    /// Measure a view according to the rules of a panel.
    /// </summary>
    public static Size MeasurePanel(this View view, MeasureContext measure, Size available)
    {
        var windowMeasured = new Size();
        foreach (var child in view.Children)
        {
            var viewIsVisible = child.GetStyle(Zui.IsVisible).Or(true);
            if (!viewIsVisible)
                continue;

            child.Measure(available, measure);
            var childMeasured = child.DesiredTotalSize;
            windowMeasured.Width = Math.Max(windowMeasured.Width, childMeasured.Width);
            windowMeasured.Height = Math.Max(windowMeasured.Height, childMeasured.Height);
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
            if (child.GetStyle(Zui.IsVisible).Or(true))
                child.Arrange(contentRect, measure);
    }

}
