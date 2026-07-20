using ZurfurGui.Base;
using ZurfurGui.Layout;
using ZurfurGui.Property;

namespace ZurfurGui.Controls;

public partial class TextView : Controllable
{
    public const double LINE_SPACING = 1.2;
    public const double TEXT_BASELINE = 0.8; // Expand lines over the size of the font itself

    public TextView()
    {
        InitializeControl();
        View.Render = TextViewRenderer.Instance;
        View.Layout = new LayoutText();
    }
}
