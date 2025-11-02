using ZurfurGui.Base;
using ZurfurGui.Render;
using ZurfurGui.Layout;

namespace ZurfurGui.Controls;

public partial class Text : Controllable
{
    public const double LINE_SPACING = 1.2;
    public const double TEXT_BASELINE = 0.8; // Expand lines over the size of the font itself

    public Text()
    {
        InitializeControl();
        View.Draw = DrawText.Instance;
        View.Layout = LayoutText.Instance;
    }

}
