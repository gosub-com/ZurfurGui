using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using ZurfurGui.Render;

namespace ZurfurGui.Controls;

public class Row : Controllable
{
    public static readonly PropertyIndex<bool> Wrap = new("Wrap");
    public static readonly PropertyIndex<Size> Spacing = new("Spacing");


    List<Controllable> _controls = new();

    public string Type => "Row";
    public string Name { get; init; } = "";
    public override string ToString() => $"{Type}{(Name == "" ? "" : $":{Name}")}";
    public View View { get; private set; }
    public Properties Properties { get; init; } = new();

    public IList<Controllable> Controls
    {
        get => _controls;
        init => _controls = value.ToList();
    }


    public Row()
    {
        View = new(this);
    }

    public void PopulateView()
    {
        View.Views.Clear();
        View.Views.AddRange(_controls.Select(c => c.View));
    }


    List<Rect> mArrange = new List<Rect>();

    public Size MeasureView(Size available, MeasureContext measure)
    {
        mArrange.Clear();
        int arrangeIndex = 0;

        var wrap = Properties.Gets(Wrap) ?? false;
        available.Width = wrap ? available.Width : double.PositiveInfinity;
        var rowPosX = 0.0;
        var rowPosY = 0.0;
        var rowHeight = 0.0;
        var viewWidth = 0.0;
        var viewHeight = 0.0;
        var spacing = Properties.Gets(Spacing) ?? new Size(5,5);
        foreach (var view in View.Views)
        {
            var viewIsVisible = view.Control?.Properties.Gets(View.IsVisible) ?? true;
            if (!viewIsVisible)
                continue;

            view.Measure(available, measure);
            var childSize = view.DesiredSize;

            if (wrap && rowPosX != 0 && rowPosX + childSize.Width > available.Width)
            {
                for (var i = arrangeIndex; i < mArrange.Count; i++)
                    mArrange[i] = mArrange[i] with { Height = rowHeight };
                arrangeIndex = mArrange.Count;

                rowPosX = 0.0;
                rowPosY += rowHeight + spacing.Height;
                rowHeight = 0.0;
            }
            mArrange.Add(new(rowPosX, rowPosY, childSize.Width, 0));
            viewWidth = Math.Max(viewWidth, rowPosX + childSize.Width);
            viewHeight = Math.Max(viewHeight, rowPosY + childSize.Height);

            rowPosX += childSize.Width + spacing.Width;
            rowHeight = Math.Max(rowHeight, childSize.Height);
        }
        for (var i = arrangeIndex; i < mArrange.Count; i++)
            mArrange[i] = mArrange[i] with { Height = rowHeight };

        return new Size(viewWidth, viewHeight);
    }

    public Size ArrangeViews(Size final)
    {
        Debug.Assert(mArrange.Count  == View.Views.Count);
        for (int i  = 0;  i < mArrange.Count; i++)
        {
            View.Views[i].Arrange(mArrange[i]);
        }
        return final;
    }
}
