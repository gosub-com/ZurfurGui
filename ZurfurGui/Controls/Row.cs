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
    public Size Measure(Size available)
    {
        return new Size(50, 20);
    }

    /// <summary>
    /// TBD: Use column algorithm (for now each item has a width of 50)
    /// </summary>
    public Size ArrangeChildren(Size final)
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

}
