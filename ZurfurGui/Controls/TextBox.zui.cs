using ZurfurGui.Draw;
using ZurfurGui.Layout;

namespace ZurfurGui.Controls;

public partial class TextBox : Controllable
{
    public TextBox()
    {
        InitializeControl();
    }
    public Layoutable DefaultLayout => LayoutText.Instance;
    public Drawable DefaultDraw => DrawLabel.Instance;

}
