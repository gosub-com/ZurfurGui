using ZurfurGui.Base;
using ZurfurGui.Render;
using ZurfurGui.Layout;

namespace ZurfurGui.Controls;

public partial class Button : Controllable
{

    public Button()
    {
        InitializeControl();
        View.Draw = DrawText.Instance;
        View.Layout = LayoutText.Instance;
    }

}
