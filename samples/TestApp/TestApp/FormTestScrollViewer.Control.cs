using ZurfurGui.Base;
using ZurfurGui.Controls;
using ZurfurGui.Layout;

namespace TestApp;

public partial class FormTestScrollViewer
{
    View? _clickTestScrim;
    View? _clickTestMessage;

    public FormTestScrollViewer()
    {
        InitializeControl();
        buttonOffsetPlus.Click += ButtonOffsetPlus_Click;
        buttonOffsetMinus.Click += ButtonOffsetMinus_Click;
        buttonClickTest.Click += ButtonClickTest_Click;
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

    void ButtonClickTest_Click(object? sender, EventArgs e)
    {
        var appWindow = View.AppWindow;
        if (appWindow == null)
            return;

        var scrim = new Scrim();
        scrim.Dismissed += (_, _) => CloseClickTest();
        scrim.Show(appWindow);

        var message = new TextView();
        message.DataContext.Text = new TextLines("Click Tested!");
        message.View.SetProperty(Panel.Align, new(AlignHorizontal.Center, AlignVertical.Center));
        scrim.View.AddChild(message.View);

        _clickTestScrim = scrim.View;
        _clickTestMessage = message.View;
    }

    void CloseClickTest()
    {
        _clickTestMessage?.RemoveFromParent();
        _clickTestMessage = null;
        _clickTestScrim?.RemoveFromParent();
        _clickTestScrim = null;
    }

    public void OnAttach() { }

    public void OnDetach()
    {
        CloseClickTest();
    }
}
