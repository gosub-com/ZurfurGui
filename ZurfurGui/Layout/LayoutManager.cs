using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Controls;

namespace ZurfurGui.Layout;

public static class LayoutManager
{
    /// <summary>
    /// Measure a view according to the rules of a panel.
    /// </summary>
    public static Size MeasurePanel(View view, MeasureContext measure, Size available)
    {
        var windowMeasured = new Size();
        foreach (var child in view.Views)
        {
            var viewIsVisible = child.Properties.Get(Zui.IsVisible, true);
            if (!viewIsVisible)
                continue;

            child.Measure(available, measure);
            var childMeasured = child.DesiredSize;
            windowMeasured.Width = Math.Max(windowMeasured.Width, childMeasured.Width);
            windowMeasured.Height = Math.Max(windowMeasured.Height, childMeasured.Height);
        }

        return windowMeasured;
    }

    /// <summary>
    /// Arrange a view according to the rules of a panel.
    /// </summary>
    public static Size ArrangePanel(View view, MeasureContext measure, Size final, Rect contentRect)
    {
        // All children get positioned at absolute coordinates in the contentRect
        foreach (var child in view.Views)
            if (child.Properties.Get(Zui.IsVisible, true))
                child.Arrange(contentRect, measure);

        return final;
    }

}
