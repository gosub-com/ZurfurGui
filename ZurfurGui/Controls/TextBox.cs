using ZurfurGui.Render;

namespace ZurfurGui.Controls;

public class TextBox : Controllable
{
    static readonly int BORDER_WIDTH = 2;
    static readonly int PADDING = 0;

    public string Type => "Zui.TextBox";
    public override string ToString() => View.ToString();
    public View View { get; private set; }

    View _content;

    public TextBox()
    {
        View = new(this);

        // Place all content inside a panel, using margin
        // TBD: Can we do this without adding an additonal label?
        Properties label = [
            (Zui.Controller, "Zui.Label"),
            (Zui.Text, "Test TextBox"),
        ];
        _content = Helper.BuildView(label);
        View.AddView(_content);

        // TBD: This should be set via event when border thickness or padding changes
        _content.Properties.Set(Zui.Margin, new(BORDER_WIDTH + PADDING));
    }

    public void Render(RenderContext context)
    {
        var r = new Rect(new(), View.Size);

        context.FillColor = Colors.Silver;
        context.FillRect(r);

        context.LineWidth = BORDER_WIDTH;
        context.StrokeColor = Colors.Navy;
        r = r.Deflate(BORDER_WIDTH / 2);
        context.StrokeRect(r);
    }

}
