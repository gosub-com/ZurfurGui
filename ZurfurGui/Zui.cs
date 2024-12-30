using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Controls;
using ZurfurGui.Platform;
using ZurfurGui.Render;

namespace ZurfurGui;

/// <summary>
/// Common Zurfur GUI constants
/// </summary>
public static class Zui
{
    // Basic View functionality
    public static readonly PropertyKey<string> Name = new("Zui.Name");
    public static readonly PropertyKey<string> Controller = new("Zui.Controller");
    public static readonly PropertyKey<Properties[]> Content = new("Zui.Content");
    public static readonly PropertyKey<bool> IsVisible = new("Zui.IsVisible");
    public static readonly PropertyKey<AlignHorizontal> AlignHorizontal = new("Zui.AlignHorizontal");
    public static readonly PropertyKey<AlignVertical> AlignVertical = new("Zui.AlignVertical");
    public static readonly PropertyKey<Thickness> Margin = new("Zui.Margin");
    public static readonly PropertyKey<Size> Size = new("Zui.Size");
    public static readonly PropertyKey<Size> SizeMax = new("Zui.SizeMax");
    public static readonly PropertyKey<Size> SizeMin = new("Zui.SizeMin");
    public static readonly PropertyKey<double> Magnification = new("Zui.Magnification");

    // Common UI interaction
    public static readonly PropertyKey<bool> DisableHitTest = new("Zui.DisableHitTest");
    public static readonly PropertyKey<Action<PointerEvent>> PointerDown = new("Zui.PointerDown");
    public static readonly PropertyKey<Action<PointerEvent>> PointerMove = new("Zui.PointerMove");
    public static readonly PropertyKey<Action<PointerEvent>> PointerUp = new("Zui.PointerUp");
    public static readonly PropertyKey<Action<PointerEvent>> PreviewPointerDown = new("Zui.PreviewPointerDown");
    public static readonly PropertyKey<Action<PointerEvent>> PreviewPointerMove = new("Zui.PreviewPointerMove");
    public static readonly PropertyKey<Action<PointerEvent>> PreviewPointerUp = new("Zui.PreviewPointerUp");

    // Label, Window, Button, Checkbox, etc.
    public static readonly PropertyKey<string> Text = new("Zui.Text");
    public static readonly PropertyKey<string> FontName = new("Zui.FontName");
    public static readonly PropertyKey<double> FontSize = new("Zui.FontSize");

    // Borders, text, misc
    public static readonly PropertyKey<Color> Background = new("Zui.Background");
    public static readonly PropertyKey<Thickness> Padding = new("Zui.Padding");
    public static readonly PropertyKey<Color> BorderColor = new("Zui.BorderColor");
    public static readonly PropertyKey<double> BorderWidth = new("Zui.BorderWidth");
    public static readonly PropertyKey<double> BorderRadius = new("Zui.BorderRadius");

    // Row, Column, DockPanel, etc.
    public static readonly PropertyKey<bool> Wrap = new("Zui.Wrap");
    public static readonly PropertyKey<Size> Spacing = new("Zui.Spacing");
    public static readonly PropertyKey<Dock> Dock = new("Zui.Dock");

}
