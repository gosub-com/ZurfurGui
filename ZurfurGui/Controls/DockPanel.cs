using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Render;

namespace ZurfurGui.Controls;

public enum Dock
{
    Left,
    Bottom,
    Right,
    Top
}

public class DockPanel : Controllable
{
    int _lastVisibleIndex;

    public string Type => "Zui.DockPanel";
    public override string ToString() => View.ToString();
    public View View { get; private set; }
    public DockPanel()
    {
        View = new(this);
    }

    public void Build()
    {
        Helper.BuildViews(View, View.Properties.Get(Zui.Content));
    }

    public Size MeasureView(Size available, MeasureContext measure)
    {
        available = available.Min(new(1000000, 1000000));  // Rule out infinities

        var constraint = available;
        _lastVisibleIndex = -1;
        var minSize = new Size();
        var views = View.Views;
        for (var i = 0;  i < views.Count;  i++)
        {
            // Ignore invisible views
            var view = views[i];
            var viewIsVisible = view.Properties.Get(Zui.IsVisible, true);
            if (!viewIsVisible)
                continue;

            _lastVisibleIndex = i;
            view.Measure(constraint, measure);
            var childSize = view.DesiredSize;
            var dock = view.Properties.Get(Zui.Dock);
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

        return (available - constraint).Max(minSize).Min(available);
    }

    public Size ArrangeViews(Size final, MeasureContext measure)
    {
        var left = 0.0;
        var right = final.Width;
        var top = 0.0;
        var bottom = final.Height;
        var views = View.Views;
        for (var i = 0;  i < views.Count;  i++)
        {
            // Ignore invisible views
            var view = views[i];
            var viewIsVisible = view.Properties.Get(Zui.IsVisible, true);
            if (!viewIsVisible)
                continue;
            
            // Default to take remaining space
            var childLocation = new Rect(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));

            // Update based on dock (last visible index always takes remaining space)
            var childSize = view.DesiredSize;
            var dock = view.Properties.Get(Zui.Dock);
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
            view.Arrange(childLocation, measure);
        }

        return final;
    }

}