using ZurfurGui.Base;
using ZurfurGui.Controls;
using ZurfurGui.Input;

namespace TestApp;

public partial class FormTestScrollViewer
{
    public FormTestScrollViewer()
    {
        InitializeControl();
        buttonOffsetPlus.View.AddEvent(Panel.PointerClick, ButtonOffsetPlus_Click);
        buttonOffsetMinus.View.AddEvent(Panel.PointerClick, ButtonOffsetMinus_Click);
    }

    void ButtonOffsetPlus_Click(object? sender, PointerEvent e)
    {
        var offset = scrollViewer.ScrollOffset;
        scrollViewer.ScrollOffset = new Point(offset.X + 16, offset.Y + 8);
    }

    void ButtonOffsetMinus_Click(object? sender, PointerEvent e)
    {
        var offset = scrollViewer.ScrollOffset;
        scrollViewer.ScrollOffset = new Point(offset.X - 16, offset.Y - 8);
    }
}
