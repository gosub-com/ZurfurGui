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
    const double LINE_SPACING = 1;


    /// <summary>
    /// Create views from properties
    /// </summary>
    public static View []BuildViews(Properties[]? controllerProperties)
    {
        if (controllerProperties == null)
            return Array.Empty<View>();

        var views = new View[controllerProperties.Length];
        for (int i = 0;  i < views.Length; i++)
            views[i] = BuildView(controllerProperties[i]);
        return views;
    }

    /// <summary>
    /// Create a view from property
    /// </summary>
    public static View BuildView(Properties controllerProperties)
    {
        var controlName = controllerProperties.Get(ZGui.Controller) ?? "";
        if (controlName == "")
            throw new ArgumentException($"The control's controller property is not specified.");

        var control = ControlRegistry.Create(controlName);
        if (control == null)
            throw new ArgumentException($"'{controlName}' is not a registered control");


        control.View.Properties = controllerProperties;
        control.Build();

        foreach (var v in control.View.Views)
            v.SetParentView(control.View);

        return control.View;
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

    public static Size MeasureText(MeasureContext measure, View view)
    {
        var fontName = view.Properties.Get(ZGui.FontName) ?? "Arial";
        var fontSize = view.Properties.Get(ZGui.FontSize, 16.0);
        var text = view.Properties.Get(ZGui.Text) ?? "";
        var lines = text.Split("\n");
        var maxWidth = lines.Max(line => measure.MeasureTextWidth(fontName, fontSize, line));
        return new Size(maxWidth, lines.Length * LINE_SPACING * fontSize);
    }


}
