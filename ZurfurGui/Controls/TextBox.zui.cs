using ZurfurGui.Draw;
using ZurfurGui.Layout;

namespace ZurfurGui.Controls;

public partial class TextBox : Controllable
{
    public TextBox()
    {
        InitializeComponent();
    }
    public Layoutable DefaultLayout => LayoutLabel.Instance;
    public Drawable DefaultDraw => DrawLabel.Instance;

}
