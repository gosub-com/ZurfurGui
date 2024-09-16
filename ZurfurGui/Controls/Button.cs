
namespace ZurfurGui.Controls;

public class Button : Controllable
{
    public string Type => "Button";
    public string Name { get; init; } = "";
    public override string ToString() => $"{Type}{(Name == "" ? "" : $":{Name}")}";
    public View View { get; private set; }

    public IList<Controllable> Controls
    {
        get => Array.Empty<Controllable>();
        init => throw new NotImplementedException("Buttons may not have controls");
    }

    public string Text { get; set; } = "";

    public Button()
    {
        View = new(this);
    }

    public void PopulateView()
    {
    }

    /// <summary>
    /// Dummy measurement until we get fonts (20 pixels per character for now)
    /// </summary>
    public Size Measure(Size available)
    {
        var lines = Text.Split("\n");
        var y = lines.Length;
        var x = lines.Max(l => l.Length);
        return new Size(20 * x, 20 * y);
    }
    public Size ArrangeChildren(Size final) => final;

}
