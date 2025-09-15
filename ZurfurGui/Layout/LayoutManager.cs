using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Base;
using ZurfurGui.Controls;
using static ZurfurGui.Base.Size;

namespace ZurfurGui.Layout;

public static class LayoutManager
{
    /// <summary>
    /// Clamp the requestedSize to be within the view's SizeMin..SizeMax property constraints.
    /// Uses the view's SizeRequest property if it's avaliable and ignoreSizeRequest is false.
    /// NOTE: Always >= 0 and SizeMin (even if Size < SizeMin)
    /// </summary>
    public static Size ClampViewSize(View v, Size requestedSize, bool ignoreSizeRequest = false)
    {
        var size = ignoreSizeRequest ? null : v.GetStyle(Zui.SizeRequest, null);
        var sizeHigh = size.Or(double.PositiveInfinity);
        var sizeLow = size.Or(0);
        var sizeMax = v.GetStyle(Zui.SizeMax, null).Or(double.PositiveInfinity);
        var sizeMin = v.GetStyle(Zui.SizeMin, null).Or(0);
        var maxSize = Max(Min(sizeHigh, sizeMax), sizeMin);
        var minSize = Max(Min(maxSize, sizeLow), sizeMin);
        return requestedSize.Clamp(minSize, maxSize);
    }

    /// <summary>
    /// Measure the size of a view, return its desired size
    /// </summary>
    internal static Size Measure(View view, Size available, MeasureContext measure)
    {
        // Include padding and border in the measurement
        var margin = view.GetStyle(Zui.Margin, null).Or(0);
        var padding = view.GetStyle(Zui.Padding, null).Or(0)
            + new Thickness(view.GetStyle(Zui.BorderWidth, null).Or(0));

        var constrained = ClampViewSize(view, available.Deflate(margin + padding));

        // Measure control content (default is a panel)
        Size measured;
        if (view.Layout is Layoutable layout)
            measured = layout.MeasureView(view.Controller.View, measure, constrained);
        else
            measured = LayoutManager.MeasurePanel(view.Controller.View, measure, constrained);

        // Desired view size includes padding and border
        measured = measured.Inflate(padding);

        // Size override
        measured = view.GetStyle(Zui.SizeRequest, null).Or(measured);

        // Max/max size override
        var sizeMax = view.GetStyle(Zui.SizeMax, null).Or(double.PositiveInfinity);
        var sizeMin = view.GetStyle(Zui.SizeMin, null).Or(0);
        measured = Max(Min(measured, sizeMax), sizeMin);

        // Min available
        measured = Min(measured, available);

        if (double.IsNaN(measured.Width) || double.IsNaN(measured.Height))
            throw new InvalidOperationException("Received NAN in Measure");

        var desiredSize = measured.Inflate(margin).MaxZero;
        return desiredSize;
    }

    /// <summary>
    /// Arrange a view, return it's final position and size
    /// </summary>
    internal static (Point position, Size size) Arrange(View view, Rect finalRect, MeasureContext measure)
    {
        var margin = view.GetStyle(Zui.Margin, null).Or(0);

        var availableSize = finalRect.Size.Deflate(margin);

        var x = finalRect.X + margin.Left;
        var y = finalRect.Y + margin.Top;
        var size = availableSize;

        var horizontalAlignment = view.GetStyle(Zui.AlignHorizontal, AlignHorizontal.Stretch);
        if (horizontalAlignment != AlignHorizontal.Stretch)
            size.Width = Math.Min(size.Width, view.DesiredSize.Width - margin.Left - margin.Right);

        var verticalAlignment = view.GetStyle(Zui.AlignVertical, AlignVertical.Stretch);
        if (verticalAlignment != AlignVertical.Stretch)
            size.Height = Math.Min(size.Height, view.DesiredSize.Height - margin.Top - margin.Bottom);

        size = ClampViewSize(view, size);

        var padding = view.GetStyle(Zui.Padding, null).Or(0)
            + new Thickness(view.GetStyle(Zui.BorderWidth, null).Or(0));

        var contentRect = new Rect(new Point(0, 0), size).Deflate(padding);

        // Arrange views
        if (view.Layout is Layoutable layout)
            size = layout.ArrangeViews(view.Controller.View, measure, size, contentRect).Min(size);
        else
            size = ArrangePanel(view.Controller.View, measure, size, contentRect).Min(size);

        switch (horizontalAlignment)
        {
            case AlignHorizontal.Center:
            case AlignHorizontal.Stretch:
                x += (availableSize.Width - size.Width) / 2;
                break;
            case AlignHorizontal.Right:
                x += availableSize.Width - size.Width;
                break;
        }

        switch (verticalAlignment)
        {
            case AlignVertical.Center:
            case AlignVertical.Stretch:
                y += (availableSize.Height - size.Height) / 2;
                break;
            case AlignVertical.Bottom:
                y += availableSize.Height - size.Height;
                break;
        }

        return (new Vector(x, y) + view.GetStyle(Zui.Offset, null).Or(0), size);
    }


    /// <summary>
    /// Measure a view according to the rules of a panel.
    /// </summary>
    public static Size MeasurePanel(View view, MeasureContext measure, Size available)
    {
        var windowMeasured = new Size();
        foreach (var child in view.Children)
        {
            var viewIsVisible = child.GetStyle(Zui.IsVisible, true);
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
        foreach (var child in view.Children)
            if (child.GetStyle(Zui.IsVisible, true))
                child.Arrange(contentRect, measure);

        return final;
    }

}
