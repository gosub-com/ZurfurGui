using ZurfurGui;
using ZurfurGui.Controls;
using ZurfurGui.Platform;

namespace TestApp;

public partial class FormSmallWinTest
{
    public FormSmallWinTest()
    {
        InitializeControl();

        bigButton.View.AddEvent(Panel.PointerClick, bigButton_Click);

    }

    void bigButton_Click(object? s, PointerEvent e)
    {

    }


}
