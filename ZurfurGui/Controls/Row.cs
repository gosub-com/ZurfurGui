using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using ZurfurGui.Render;

namespace ZurfurGui.Controls;

public partial class Row : Controllable
{
    List<Rect> _arrange = new List<Rect>();

    public Row()
    {
        InitializeComponent();
    }

    public void LoadContent()
    {
        Loader.BuildViews(View, View.Properties.Get(Zui.Content));
    }


    public Size MeasureView(Size available, MeasureContext measure)
    {
        _arrange.Clear();

        var wrap = View.Properties.Get(Zui.Wrap, false);
        available.Width = wrap ? available.Width : double.PositiveInfinity;
        var rowPosX = 0.0;
        var rowPosY = 0.0;
        var rowHeight = 0.0;
        var viewWidth = 0.0;
        var viewHeight = 0.0;
        var spacing = View.Properties.Get(Zui.Spacing, new Size(5, 5));
        int arrangeIndex = 0;
        foreach (var view in View.Views)
        {
            // Ignore invisible views
            var viewIsVisible = view.Properties.Get(Zui.IsVisible, true);
            if (!viewIsVisible)
                continue;

            view.Measure(available, measure);
            var childSize = view.DesiredSize;

            if (wrap && rowPosX != 0 && rowPosX + childSize.Width > available.Width)
            {
                // Equalize the height of the previously wrapped row
                for (var i = arrangeIndex; i < _arrange.Count; i++)
                    _arrange[i] = _arrange[i] with { Height = rowHeight };
                arrangeIndex = _arrange.Count;

                rowPosX = 0.0;
                rowPosY += rowHeight + spacing.Height;
                rowHeight = 0.0;
            }
            _arrange.Add(new(rowPosX, rowPosY, childSize.Width, 0));
            viewWidth = Math.Max(viewWidth, rowPosX + childSize.Width);
            viewHeight = Math.Max(viewHeight, rowPosY + childSize.Height);

            rowPosX += childSize.Width + spacing.Width;
            rowHeight = Math.Max(rowHeight, childSize.Height);
        }
        // Equalize the height of the final row (or everything if not wrapped)
        for (var i = arrangeIndex; i < _arrange.Count; i++)
            _arrange[i] = _arrange[i] with { Height = rowHeight };

        return new Size(viewWidth, viewHeight);
    }

    public Size ArrangeViews(Size final, MeasureContext measure)
    {
        var i = 0;
        foreach (var view in View.Views)
        {
            if (view.Properties.Get(Zui.IsVisible, true))
            {
                view.Arrange(i < _arrange.Count ? _arrange[i] : new(), measure);
                i++;
            }
        }
        Debug.Assert(i == _arrange.Count); // Visibility or child count changed between measure and arrange
        return final;
    }
}
