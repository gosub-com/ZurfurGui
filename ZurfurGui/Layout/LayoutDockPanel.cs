using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Base;

namespace ZurfurGui.Layout;

public enum Dock
{
    Left,
    Bottom,
    Right,
    Top
}

public class LayoutDockPanel : Layoutable
{
    int _lastVisibleIndex;

    public string TypeName => "DockPanel";


    public Size MeasureView(View view, MeasureContext measure, Size available)
    {
        if (available.Width >= double.MaxValue || available.Height >= double.MaxValue)
            throw new InvalidOperationException("DockPanel requires finite available size");

        var constraint = available;
        _lastVisibleIndex = -1;
        var minSize = new Size();
        var childViews = view.Children;
        for (var i = 0;  i < childViews.Count;  i++)
        {
            // Ignore invisible views
            var childView = childViews[i];
            if (!childView.GetStyle(Zui.IsVisible).Or(true))
                continue;

            _lastVisibleIndex = i;
            childView.Measure(constraint, measure);
            var childSize = childView.DesiredTotalSize;
            var dock = childView.GetStyle(Zui.Dock).Or(Dock.Left);
            switch (dock)
            {
                case Dock.Left:
                case Dock.Right:
                    constraint.Width = Math.Max(0, constraint.Width - childSize.Width);
                    minSize.Height = Math.Max(minSize.Height, available.Height - constraint.Height + childSize.Height);
                    break;
                case Dock.Top: 
                case Dock.Bottom:
                    constraint.Height = Math.Max(0, constraint.Height - childSize.Height);
                    minSize.Width = Math.Max(minSize.Width, available.Width - constraint.Width + childSize.Width);
                    break;
            }
        }

        return (available - constraint).ToSize.Max(minSize).Min(available);
    }

    public void ArrangeViews(View view, MeasureContext measure)
    {
        var contentRect = view.ContentRect;
        var left = contentRect.X;
        var right = contentRect.Right;
        var top = contentRect.Y;
        var bottom = contentRect.Bottom;
        var childViews = view.Children;
        for (var i = 0;  i < childViews.Count;  i++)
        {
            // Ignore invisible views
            var childView = childViews[i];
            var viewIsVisible = childView.GetStyle(Zui.IsVisible).Or(true);
            if (!viewIsVisible)
                continue;
            
            // Default to take remaining space
            var childLocation = new Rect(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));

            // Update based on dock (last visible index always takes remaining space)
            var childSize = childView.DesiredTotalSize;
            var dock = childView.GetStyle(Zui.Dock).Or(Dock.Left);
            if (i < _lastVisibleIndex)
            {
                switch (dock)
                {
                    case Dock.Left:
                        childLocation.Width = childSize.Width;
                        left += childSize.Width;
                        break;

                    case Dock.Right:
                        right -= childSize.Width;
                        childLocation.X = right;
                        childLocation.Width = childSize.Width;
                        break;

                    case Dock.Top:
                        childLocation.Height = childSize.Height;
                        top += childSize.Height;
                        break;

                    case Dock.Bottom:
                        bottom -= childSize.Height;
                        childLocation.Y = bottom;
                        childLocation.Height = childSize.Height;
                        break;
                }
            }
            childView.Arrange(childLocation, measure);
        }
    }

}