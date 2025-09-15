using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Base;
using ZurfurGui.Controls;
using ZurfurGui.Draw;
using ZurfurGui.Layout;
using ZurfurGui.Platform;
using ZurfurGui.Styles;

namespace ZurfurGui;

/// <summary>
/// Common Zurfur GUI constants
/// </summary>
public static class Zui
{
    /// <summary>
    /// Initialize the library with the built in controls, etc.
    /// </summary>
    public static AppWindow Init(Action<AppWindow> mainAppEntry)
    {
        // Force initialization of static properties
        _ = Zui.Name;

        ControlManager.Add(() => new Panel());
        ControlManager.Add(() => new Button());
        ControlManager.Add(() => new Text());
        ControlManager.Add(() => new TextBox());
        ControlManager.Add(() => new Window());
        ControlManager.Add(() => new AppWindow());

        var appWindow = new AppWindow();
        mainAppEntry(appWindow);
        return appWindow;
    }

    // Basic View functionality
    public static readonly PropertyKey<string> Name = new("Name");
    public static readonly PropertyKey<string> Controller = new("Controller");
    public static readonly PropertyKey<string> Layout = new("Layout");
    public static readonly PropertyKey<string> Draw = new("Draw");
    public static readonly PropertyKey<Properties[]> Content = new("Content");

    // Basic View Style
    public static readonly PropertyKey<bool> IsVisible = new("IsVisible");
    public static readonly PropertyKey<AlignHorizontal> AlignHorizontal = new("AlignHorizontal");
    public static readonly PropertyKey<AlignVertical> AlignVertical = new("AlignVertical");
    public static readonly PropertyKey<ThicknessProp> Margin = new("Margin");
    public static readonly PropertyKey<SizeProp> SizeRequest = new("SizeRequest");
    public static readonly PropertyKey<SizeProp> SizeMax = new("SizeMax");
    public static readonly PropertyKey<SizeProp> SizeMin = new("SizeMin");
    public static readonly PropertyKey<DoubleProp> Magnification = new("Magnification");
    public static readonly PropertyKey<bool> Clip = new("Clip");
    public static readonly PropertyKey<PointProp> Offset = new("Offset");
    public static readonly PropertyKey<ThicknessProp> Padding = new("Padding");
    public static readonly PropertyKey<ColorProp> Background = new("Background");
    public static readonly PropertyKey<ColorProp> BorderColor = new("BorderColor");
    public static readonly PropertyKey<DoubleProp> BorderWidth = new("BorderWidth");
    public static readonly PropertyKey<DoubleProp> BorderRadius = new("BorderRadius");

    // Style
    public static readonly PropertyKey<TextLines> Classes = new("Classes");
    public static readonly PropertyKey<StyleSheet> DefaultStyle = new("DefaultStyle");
    public static readonly PropertyKey<TextLines> UseStyle = new("UseStyle");

    // Label, Window, Button, Checkbox, etc.
    public static readonly PropertyKey<TextLines> Text = new("Text");
    public static readonly PropertyKey<string> FontName = new("FontName");
    public static readonly PropertyKey<DoubleProp> FontSize = new("FontSize");

    // Row, Column, DockPanel, etc.
    public static readonly PropertyKey<bool> Wrap = new("Wrap");
    public static readonly PropertyKey<SizeProp> Spacing = new("Spacing");
    public static readonly PropertyKey<Dock> Dock = new("Dock");

    // Common UI interaction
    public static readonly PropertyKey<bool> DisableHitTest = new("DisableHitTest");
    public static readonly PropertyKey<EventHandler<PointerEvent>> PointerDown = new("PointerDown");
    public static readonly PropertyKey<EventHandler<PointerEvent>> PointerMove = new("PointerMove");
    public static readonly PropertyKey<EventHandler<PointerEvent>> PointerUp = new("PointerUp");
    public static readonly PropertyKey<EventHandler<PointerEvent>> PreviewPointerDown = new("PreviewPointerDown");
    public static readonly PropertyKey<EventHandler<PointerEvent>> PreviewPointerMove = new("PreviewPointerMove");
    public static readonly PropertyKey<EventHandler<PointerEvent>> PreviewPointerUp = new("PreviewPointerUp");
    public static readonly PropertyKey<EventHandler> PointerCaptureLost = new("PointerCaptureLost");

}
