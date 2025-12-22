using ZurfurGui.Base;
using ZurfurGui.Layout;
using ZurfurGui.Property;
using ZurfurGui.Render;

namespace ZurfurGui.Controls;

public partial class TextView : Controllable
{
    public const double LINE_SPACING = 1.2;
    public const double TEXT_BASELINE = 0.8; // Expand lines over the size of the font itself

    public static readonly PropertyKey<TextLinesProp> Text = new("text", typeof(TextView), ViewFlags.ReMeasure);
    public static readonly PropertyKey<FontProp> Font = new("font", typeof(TextView), ViewFlags.ReMeasure);
    public static readonly PropertyKey<ColorProp> Color = new("color", typeof(TextView), ViewFlags.ReDraw);


    public TextView()
    {
        InitializeControl();
        View.Draw = DrawText.Instance;
        View.Layout = LayoutText.Instance;
    }

}
