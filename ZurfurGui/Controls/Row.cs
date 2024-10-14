using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using ZurfurGui.Render;

namespace ZurfurGui.Controls;

public class Row : Controllable
{
    List<Controllable> _controls = new();

    public string Type => "Row";
    public string Name { get; init; } = "";
    public override string ToString() => $"{Type}{(Name == "" ? "" : $":{Name}")}";
    public View View { get; private set; }

    public IList<Controllable> Controls
    {
        get => _controls;
        init => _controls = value.ToList();
    }

    /// <summary>
    /// Space between elements (Height is only used when Wrap is true)
    /// </summary>
    public Size Spacing { get; set; } = new Size(5,5);

    public bool Wrap { get; set; } = false;


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

        available.Width = Wrap ? available.Width : double.PositiveInfinity;
        var rowPosX = 0.0;
        var rowPosY = 0.0;
        var rowHeight = 0.0;
        var viewWidth = 0.0;
        var viewHeight = 0.0;
        foreach (var view in View.Views)
        {
            if (!view.IsVisible)
                continue;

            view.Measure(available, measure);
            var childSize = view.DesiredSize;

            if (Wrap && rowPosX != 0 && rowPosX + childSize.Width > available.Width)
            {
                for (var i = arrangeIndex; i < mArrange.Count; i++)
                    mArrange[i] = mArrange[i] with { Height = rowHeight };
                arrangeIndex = mArrange.Count;

                rowPosX = 0.0;
                rowPosY += rowHeight + Spacing.Height;
                rowHeight = 0.0;
            }
            mArrange.Add(new(rowPosX, rowPosY, childSize.Width, 0));
            viewWidth = Math.Max(viewWidth, rowPosX + childSize.Width);
            viewHeight = Math.Max(viewHeight, rowPosY + childSize.Height);

            rowPosX += childSize.Width + Spacing.Width;
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

    // Forward View properties
    public bool IsVisible { get => View.IsVisible; set => View.IsVisible = value; }
    public Size Size { get => View.Size; set => View.Size = value; }
    public Size SizeMax { get => View.Size; set => View.Size = value; }
    public Size SizeMin { get => View.Size; set => View.Size = value; }
    public HorizontalAlignment AlignHorizontal { get => View.AlignHorizontal; set => View.AlignHorizontal = value; }
    public VerticalAlignment AlignVertical { get => View.AlignVertical; set => View.AlignVertical = value; }
    public Thickness Margin { get => View.Margin; set => View.Margin = value; }
}
