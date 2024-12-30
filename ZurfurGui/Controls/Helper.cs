using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Render;

namespace ZurfurGui.Controls;


public static class Helper
{
    const double LINE_SPACING = 1.2;

    // Expand lines over the size of the font itself
    const double TEXT_BASELINE = 0.8;

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
        var controlName = controllerProperties.Get(Zui.Controller) ?? "";
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

    /// <summary>
    /// Measure a view according to the rules of a panel
    /// </summary>
    public static Size MeasureViewPanel(View view, Size available, MeasureContext measure, Thickness padding = default)
    {
        available = available.Deflate(padding);
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
        return windowMeasured.Inflate(padding);
    }

    /// <summary>
    /// Arrange a view according to the rules of a panel
    /// </summary>
    public static Size ArrangeViewPanel(View view, Size final, MeasureContext measure, Thickness padding = default)
    {
        var layoutRect = new Rect(0, 0, final.Width, final.Height).Deflate(padding);
        foreach (var child in view.Views)
            if (child.Properties.Get(Zui.IsVisible, true))
                child.Arrange(layoutRect, measure);
        return final;
    }


    public static Size MeasureText(MeasureContext measure, View view)
    {
        var fontName = view.Properties.Get(Zui.FontName) ?? "Arial";
        var fontSize = view.Properties.Get(Zui.FontSize, 16.0);
        var text = view.Properties.Get(Zui.Text) ?? "";
        var lines = text.Split("\n");
        var maxWidth = lines.Max(line => measure.MeasureTextWidth(fontName, fontSize, line));
        return new Size(maxWidth, lines.Length * LINE_SPACING * fontSize);
    }

    public static void RenderText(View view, RenderContext context, Color color)
    {
        var text = view.Properties.Get(Zui.Text) ?? "";
        var fontName = view.Properties.Get(Zui.FontName) ?? "Arial";
        var fontSize = view.Properties.Get(Zui.FontSize, 16.0);
        context.FontName = fontName;
        context.FontSize = fontSize;
        context.FillColor = color;

        var lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
            context.FillText(lines[i], 0, fontSize * TEXT_BASELINE + i * fontSize * LINE_SPACING + (LINE_SPACING-1)*fontSize*0.5);

    }



}
