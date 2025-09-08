
using System.Reflection.Emit;
using ZurfurGui.Layout;
using ZurfurGui.Platform;
using ZurfurGui.Draw;

namespace ZurfurGui.Controls;

public partial class Label : Controllable
{
    public const double LINE_SPACING = 1.2;
    public const double TEXT_BASELINE = 0.8; // Expand lines over the size of the font itself

    public Label()
    {
        InitializeComponent();
    }

    public Layoutable DefaultLayout => LayoutLabel.Instance;
    public Drawable DefaultDraw => DrawLabel.Instance;

    


}
