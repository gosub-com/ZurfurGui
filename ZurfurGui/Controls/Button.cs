﻿
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
    public Size MeasureView(Size available)
    {
        var lines = Text.Split("\n");
        var y = lines.Length;
        var x = lines.Max(l => l.Length);
        return new Size(20 * x, 20 * y);
    }
    public Size ArrangeViews(Size final) => final;


    // Forward View properties
    public bool IsVisible { get => View.IsVisible; set => View.IsVisible = value; }
    public Size Size { get => View.Size; set => View.Size = value; }
    public Size MaxSize { get => View.Size; set => View.Size = value; }
    public Size MinSize { get => View.Size; set => View.Size = value; }
    public HorizontalAlignment HorizontalAlignment { get => View.HorizontalAlignment; set => View.HorizontalAlignment = value; }
    public VerticalAlignment VerticalAlignment { get => View.VerticalAlignment; set => View.VerticalAlignment = value; }
    public Thickness Margin { get => View.Margin; set => View.Margin = value; }
}
