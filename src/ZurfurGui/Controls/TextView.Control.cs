using ZurfurGui.Base;
using ZurfurGui.Layout;
using ZurfurGui.Property;

namespace ZurfurGui.Controls;

public partial class TextView : Controllable
{
    public TextView()
    {
        InitializeControl();
        View.Render = TextViewRenderer.Instance;
        View.Layout = new LayoutText();
    }
}
