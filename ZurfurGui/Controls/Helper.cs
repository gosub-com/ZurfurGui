using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Render;

namespace ZurfurGui.Controls;


public static class Helper
{

    /// <summary>
    /// Create controllers from properties, add to views
    /// </summary>
    public static void BuildViewsFromProperties(IList<View> views, Properties[]? controllerProperties)
    {
        if (controllerProperties != null)
            foreach (var control in controllerProperties)
                views.Add(BuildViewFromProperties(control));
    }

    public static View BuildViewFromProperties(Properties controllerProperties)
    {
        var controlName = controllerProperties.Get(ZGui.Controller) ?? "";
        if (controlName == "")
            throw new ArgumentException($"The control's controller property is not specified.");

        var control = ControlRegistry.Create(controlName);
        if (control == null)
            throw new ArgumentException($"'{controlName}' is not a registered control");


        control.View.Properties = controllerProperties;
        return control.BuildView(controllerProperties);
    }

    public static Size MeasurePanel(IEnumerable<View> views, Size available, MeasureContext measure)
    {
        var windowMeasured = new Size();
        foreach (var view in views)
        {
            var viewIsVisible = view.Properties.Get(ZGui.IsVisible, true);
            if (!viewIsVisible)
                continue;

            view.Measure(available, measure);
            var childMeasured = view.DesiredSize;
            windowMeasured.Width = Math.Max(windowMeasured.Width, childMeasured.Width);
            windowMeasured.Height = Math.Max(windowMeasured.Height, childMeasured.Height);
        }
        return windowMeasured;
    }
    public static Size ArrangePanel(IReadOnlyList<View> views, Size final, MeasureContext measure)
    {
        var layoutRect = new Rect(0, 0, final.Width, final.Height);
        foreach (var view in views)
            view.Arrange(layoutRect, measure);

        return final;
    }

}
