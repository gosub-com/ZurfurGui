using ZurfurGui.Base;
using ZurfurGui.Draw;
using ZurfurGui.Layout;

namespace ZurfurGui.Controls;

public partial class TextBox : Controllable
{
    public TextBox()
    {
        InitializeControl();
        View.Draw = DrawText.Instance;
        View.Layout = LayoutText.Instance;
    }
}
