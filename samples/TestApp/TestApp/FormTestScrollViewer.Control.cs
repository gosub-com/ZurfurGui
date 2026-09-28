using ZurfurGui.Base;
using ZurfurGui.Controls;

namespace TestApp;

public partial class FormTestScrollViewer
{
    public FormTestScrollViewer()
    {
        InitializeControl();
        buttonOffsetPlus.Click += ButtonOffsetPlus_Click;
        buttonOffsetMinus.Click += ButtonOffsetMinus_Click;
    }

    void ButtonOffsetPlus_Click(object? sender, EventArgs e)
    {
        var offset = scrollViewer.ScrollOffset;
        scrollViewer.ScrollOffset = new Point(offset.X + 16, offset.Y + 8);
    }

    void ButtonOffsetMinus_Click(object? sender, EventArgs e)
    {
        var offset = scrollViewer.ScrollOffset;
        scrollViewer.ScrollOffset = new Point(offset.X - 16, offset.Y - 8);
    }
}
