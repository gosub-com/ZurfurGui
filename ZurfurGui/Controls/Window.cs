namespace ZurfurGui.Controls;

public class Window : Controllable
{
    List<Controllable> _controls = new();

    public string Type => "Window";
    public string Name { get; init; } = "";
    public override string ToString() => $"{Type}{(Name == "" ? "" : $":{Name}")}";
    public View View { get; private set; }

    public IList<Controllable> Controls
    {
        get => _controls;
        init => _controls = value.ToList();
    }

    public Window()
    {
        View = new(this);
    }

    public string Text { get; set; } = "";

    public void PopulateView()
    {
        View.Views.Clear();
        View.Views.AddRange(_controls.Select(c => c.View));
    }

    /// <summary>
    /// TBD: Todo
    /// </summary>
    public Size Measure(Size available)
    {
        return available;
    }

    /// <summary>
    /// A window puts all controls at (0,0), like a canvas.  Position can be controlled using margin.
    /// </summary>
    public Size ArrangeChildren(Size final)
    {
        foreach (var view in View.Views)
            view.Arrange(new Rect(0, 0, 
                Math.Max(final.Width, view.DesiredSize.Width), Math.Max(final.Height, view.DesiredSize.Height)));

        return final;
    }
}
