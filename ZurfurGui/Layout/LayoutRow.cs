
using System.Diagnostics;
using ZurfurGui.Controls;

namespace ZurfurGui.Layout;

public class LayoutRow : Layoutable
{
    List<Rect> _measuredRects = new List<Rect>();


    public string LayoutType => "Row";


    public Size MeasureView(View view, MeasureContext measure, Size available)
    {
        _measuredRects.Clear();

        var wrap = view.Properties.Get(Zui.Wrap, false);
        available.Width = wrap ? available.Width : double.PositiveInfinity;
        var rowPosX = 0.0;
        var rowPosY = 0.0;
        var rowHeight = 0.0;
        var viewWidth = 0.0;
        var viewHeight = 0.0;
        var spacing = view.Properties.Get(Zui.Spacing, new Size(5, 5));
        int arrangeIndex = 0;
        foreach (var childView in view.Views)
        {
            // Ignore invisible views
            var viewIsVisible = childView.Properties.Get(Zui.IsVisible, true);
            if (!viewIsVisible)
                continue;

            childView.Measure(available, measure);
            var childSize = childView.DesiredSize;

            if (wrap && rowPosX != 0 && rowPosX + childSize.Width > available.Width)
            {
                // Equalize the height of the previously wrapped row
                for (var i = arrangeIndex; i < _measuredRects.Count; i++)
                    _measuredRects[i] = _measuredRects[i] with { Height = rowHeight };
                arrangeIndex = _measuredRects.Count;

                rowPosX = 0.0;
                rowPosY += rowHeight + spacing.Height;
                rowHeight = 0.0;
            }
            _measuredRects.Add(new(rowPosX, rowPosY, childSize.Width, 0));
            viewWidth = Math.Max(viewWidth, rowPosX + childSize.Width);
            viewHeight = Math.Max(viewHeight, rowPosY + childSize.Height);

            rowPosX += childSize.Width + spacing.Width;
            rowHeight = Math.Max(rowHeight, childSize.Height);
        }
        // Equalize the height of the final row (or everything if not wrapped)
        for (var i = arrangeIndex; i < _measuredRects.Count; i++)
            _measuredRects[i] = _measuredRects[i] with { Height = rowHeight };

        return new Size(viewWidth, viewHeight);
    }

    public Size ArrangeViews(View view, MeasureContext measure, Size final, Rect contentRect)
    {
        var i = 0;
        foreach (var childView in view.Views)
        {
            if (childView.Properties.Get(Zui.IsVisible, true))
            {
               
                var measuredRect = i < _measuredRects.Count ? _measuredRects[i] : new();
                childView.Arrange(measuredRect.Move(contentRect.Position), measure);
                i++;
            }
        }
        Debug.Assert(i == _measuredRects.Count); // Visibility or child count changed between measure and arrange
        return final;
    }
}
