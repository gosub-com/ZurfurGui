using ZurfurGui;
using ZurfurGui.Controls;

namespace TestApp;

public partial class FormSmallWinTest
{
    public FormSmallWinTest()
    {
        InitializeControl();

        bigButton.Click += BigButton_Click;

        buttonVisibilityTest.Click += (s, e) =>
        {
            textVisibilityTest.View.IsVisible = !textVisibilityTest.View.IsVisible;
        };

    }

    void BigButton_Click(object? s, EventArgs e)
    {

    }


}
