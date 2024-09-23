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

    public Row()
    {
        View = new(this);
    }

    public void PopulateView()
    {
        View.Views.Clear();
        View.Views.AddRange(_controls.Select(c => c.View));
    }

    /// <summary>
    /// TBD: Use column algorithm (for now, each item is 50,20)
    /// </summary>
    /// <param name="available"></param>
    /// <returns></returns>
    public Size MeasureView(Size available)
    {
        return new Size(50, 20);
    }

    /// <summary>
    /// TBD: Use column algorithm (for now each item has a width of 50)
    /// </summary>
    public Size ArrangeViews(Size final)
    {
        var x = 0;
        foreach (var view in View.Views)
        {
            view.Arrange(new Rect(x, 0,
                Math.Max(final.Width, view.DesiredSize.Width), Math.Max(final.Height, view.DesiredSize.Height)));
            x += 50;
        }
        return final;
    }

    // Forward View properties
    public bool IsVisible { get => View.IsVisible; set => View.IsVisible = value; }
    public Size Size { get => View.Size; set => View.Size = value; }
    public Size MaxSize { get => View.Size; set => View.Size = value; }
    public Size MinSize { get => View.Size; set => View.Size = value; }
    public HorizontalAlignment HorizontalAlignment { get => View.HorizontalAlignment; set => View.HorizontalAlignment = value; }
    public VerticalAlignment VerticalAlignment { get => View.VerticalAlignment; set => View.VerticalAlignment = value; }
    public Thickness Margin { get => View.Margin; set => View.Margin = value; }
}
