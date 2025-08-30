using ZurfurGui.Render;

namespace ZurfurGui.Controls;

public partial class TextBox : Controllable
{
    static readonly int BORDER_WIDTH = 2;
    static readonly int PADDING = 0;


    public TextBox()
    {
        InitializeComponent();
    }

    public void LoadContent()
    {
        // Place all content inside a panel, using margin
        // TBD: Can we do this without adding an additonal label?
        Properties label = [
            (Zui.Controller, "Label"),
            (Zui.Text,  View.Properties.Get(Zui.Text) ?? ""),
        ];
        var content = Loader.BuildView(label);
        View.AddView(content);

        // TBD: This should be set via event when border thickness or padding changes
        content.Properties.Set(Zui.Margin, new(BORDER_WIDTH + PADDING));

    }

    public void Render(RenderContext context)
    {
        var r = new Rect(new(), View.Size);

        context.FillColor = Colors.LightBlue;
        context.FillRect(r);

        context.LineWidth = BORDER_WIDTH;
        context.StrokeColor = Colors.Navy;
        r = r.Deflate(BORDER_WIDTH / 2);
        context.StrokeRect(r);
    }

}
