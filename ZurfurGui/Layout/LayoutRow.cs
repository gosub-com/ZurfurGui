
using System.Diagnostics;
using ZurfurGui.Base;
using ZurfurGui.Controls;
using ZurfurGui.Property;

namespace ZurfurGui.Layout;


/// <summary>
/// Layout rows.  TBD: This will do column as well.
/// </summary>
public class LayoutRow : Layoutable
{

    public static readonly PropertyKeyMerge<EnumProp<bool>> Wrap = new("cell.wrap", typeof(LayoutRow), new(), ViewFlags.ReMeasure);
    public static readonly PropertyKeyMerge<SizeProp> Spacing = new("cell.spacing", typeof(LayoutRow), new(), ViewFlags.ReMeasure);

    readonly Size DEFAULT_SIZE = new Size(5, 5);

    List<Rect> _measuredRects = new List<Rect>();

    public string TypeName => "Row";



    public Size MeasureView(View view, MeasureContext measure, Size available)
    {
        _measuredRects.Clear();

        var wrap = view.GetStyle(Wrap).Or(true);
        var rowPosX = 0.0;
        var rowPosY = 0.0;
        var rowHeight = 0.0;
        var viewWidth = 0.0;
        var viewHeight = 0.0;
        var spacing = view.GetStyle(Spacing).Or(DEFAULT_SIZE);
        int arrangeIndex = 0;
        foreach (var childView in view.Children)
        {
            // Ignore invisible views
            var viewIsVisible = childView.GetStyle(Panel.IsVisible);
            if (!viewIsVisible)
                continue;

            childView.Measure(available, measure);
            var childSize = childView.DesiredTotalSize;

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

    public void ArrangeViews(View view, MeasureContext measure)
    {
        var i = 0;
        var contentRect = view.ContentRect;
        foreach (var childView in view.Children)
        {
            if (childView.GetStyle(Panel.IsVisible))
            {
               
                var measuredRect = i < _measuredRects.Count ? _measuredRects[i] : new();
                childView.Arrange(measuredRect.Move(contentRect.Position.ToVector), measure);
                i++;
            }
        }
        Debug.Assert(i == _measuredRects.Count); // Visibility or child count changed between measure and arrange
    }
}
