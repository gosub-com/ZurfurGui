using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Controls;
using ZurfurGui.Render;

namespace ZurfurGui;

/// <summary>
/// Common Zurfur GUI constants
/// </summary>
public static class ZGui
{
    public static readonly PropertyKey<string> Name = new("ZGui.Name");
    public static readonly PropertyKey<string> Controller = new("ZGui.Controller");
    public static readonly PropertyKey<Properties[]> Controls = new("ZGui.Controls");
    public static readonly PropertyKey<bool> IsVisible = new("ZGui.IsVisible");
    public static readonly PropertyKey<HorizontalAlignment> AlignHorizontal = new("ZGui.AlignHorizontal");
    public static readonly PropertyKey<VerticalAlignment> AlignVertical = new("ZGui.AlignVertical");
    public static readonly PropertyKey<string> Text = new("ZGui.Text");
    public static readonly PropertyKey<Thickness> Margin = new("ZGui.Margin");
    public static readonly PropertyKey<Size> Size = new("ZGui.Size");
    public static readonly PropertyKey<Size> SizeMax = new("ZGui.SizeMax");
    public static readonly PropertyKey<Size> SizeMin = new("ZGui.SizeMin");
    public static readonly PropertyKey<string> FontName = new("ZGui.FontName");
    public static readonly PropertyKey<double> FontSize = new("ZGui.FontSize");
    public static readonly PropertyKey<bool> Wrap = new("ZGui.Wrap");
    public static readonly PropertyKey<Size> Spacing = new("ZGui.Spacing");
    public static readonly PropertyKey<double> Magnification = new("ZGui.Magnification");
}
