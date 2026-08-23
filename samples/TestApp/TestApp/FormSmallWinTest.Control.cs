using ZurfurGui;
using ZurfurGui.Controls;
using ZurfurGui.Input;

namespace TestApp;

public partial class FormSmallWinTest
{
    public FormSmallWinTest()
    {
        InitializeControl();

        bigButton.View.AddEvent(Panel.PointerClick, bigButton_Click);

        buttonVisibilityTest.View.AddEvent(Panel.PointerClick, (s, e) =>
        {
            textVisibilityTest.View.IsVisible = !textVisibilityTest.View.IsVisible;
        });

    }

    void bigButton_Click(object? s, PointerEvent e)
    {

    }


}
