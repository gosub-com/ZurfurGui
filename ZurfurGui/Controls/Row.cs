using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using ZurfurGui.Render;

namespace ZurfurGui.Controls;

public class Row : Controllable
{
    List<Rect> _arrange = new List<Rect>();

    public string Type => "Row";
    public string Name { get; init; } = "";
    public override string ToString() => $"{Type}{(Name == "" ? "" : $":{Name}")}";
    public View View { get; private set; }
    public Properties Properties { get; set; } = new();


    public Row()
    {
        View = new(this);
    }

    public View BuildView(Properties properties)
    {
        View.Views.Clear();
        ViewHelper.AddControllers(View.Views, properties.Getc(ZGui.Controls));
        return View;
    }


    public Size MeasureView(Size available, MeasureContext measure)
    {
        _arrange.Clear();
        int arrangeIndex = 0;

        var wrap = Properties.Gets(ZGui.Wrap) ?? false;
        available.Width = wrap ? available.Width : double.PositiveInfinity;
        var rowPosX = 0.0;
        var rowPosY = 0.0;
        var rowHeight = 0.0;
        var viewWidth = 0.0;
        var viewHeight = 0.0;
        var spacing = Properties.Gets(ZGui.Spacing) ?? new Size(5,5);
        foreach (var view in View.Views)
        {
            var viewIsVisible = view.Control?.Properties.Gets(ZGui.IsVisible) ?? true;
            if (!viewIsVisible)
                continue;

            view.Measure(available, measure);
            var childSize = view.DesiredSize;

            if (wrap && rowPosX != 0 && rowPosX + childSize.Width > available.Width)
            {
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
        for (var i = arrangeIndex; i < _arrange.Count; i++)
            _arrange[i] = _arrange[i] with { Height = rowHeight };

        return new Size(viewWidth, viewHeight);
    }

    public Size ArrangeViews(Size final)
    {
        Debug.Assert(_arrange.Count  == View.Views.Count);
        for (int i  = 0;  i < _arrange.Count; i++)
        {
            View.Views[i].Arrange(_arrange[i]);
        }
        return final;
    }
}
