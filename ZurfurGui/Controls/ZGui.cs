using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZurfurGui.Controls;

/// <summary>
/// Common Zurfur GUI constants
/// </summary>
public static class ZGui
{
    public static readonly PropertyKey<string> Name = new("Name");
    public static readonly PropertyKey<string> Controller = new("Controller");
    public static readonly PropertyKey<Properties[]> Controls = new("Controls");
    public static readonly PropertyKey<bool> IsVisible = new("IsVisible");
    public static readonly PropertyKey<HorizontalAlignment> AlignHorizontal = new("AlignHorizontal");
    public static readonly PropertyKey<VerticalAlignment> AlignVertical = new("AlignVertical");
    public static readonly PropertyKey<string> Text = new("Text");
    public static readonly PropertyKey<Thickness> Margin = new("Margin");
    public static readonly PropertyKey<Size> Size = new("Size");
    public static readonly PropertyKey<Size> SizeMax = new("SizeMax");
    public static readonly PropertyKey<Size> SizeMin = new("SizeMin");

    public static readonly PropertyKey<string> FontName = new("FontName");
    public static readonly PropertyKey<double> FontSize = new("FontSize");

    public static readonly PropertyKey<bool> Wrap = new("Wrap");
    public static readonly PropertyKey<Size> Spacing = new("Spacing");
}
