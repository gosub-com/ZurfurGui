using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Controls;
using ZurfurGui.Platform;

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

        ControlRegistry.Add(() => new Panel());
        ControlRegistry.Add(() => new Button());
        ControlRegistry.Add(() => new Column());
        ControlRegistry.Add(() => new Label());
        ControlRegistry.Add(() => new Row());
        ControlRegistry.Add(() => new Border());
        ControlRegistry.Add(() => new TextBox());
        ControlRegistry.Add(() => new DockPanel());
        ControlRegistry.Add(() => new Window());
        ControlRegistry.Add(() => new AppWindow());

        var appWindow = new AppWindow();
        mainAppEntry(appWindow);
        return appWindow;
    }


    // Basic View functionality
    public static readonly PropertyKey<string> Name = new("Name");
    public static readonly PropertyKey<string> Controller = new("Controller");
    public static readonly PropertyKey<string> Component = new("Component");
    public static readonly PropertyKey<Properties[]> Content = new("Content");
    public static readonly PropertyKey<bool> IsVisible = new("IsVisible");
    public static readonly PropertyKey<AlignHorizontal> AlignHorizontal = new("AlignHorizontal");
    public static readonly PropertyKey<AlignVertical> AlignVertical = new("AlignVertical");
    public static readonly PropertyKey<Thickness> Margin = new("Margin");
    public static readonly PropertyKey<Size> Size = new("Size");
    public static readonly PropertyKey<Size> SizeMax = new("SizeMax");
    public static readonly PropertyKey<Size> SizeMin = new("SizeMin");
    public static readonly PropertyKey<double> Magnification = new("Magnification");

    // Common UI interaction
    public static readonly PropertyKey<bool> DisableHitTest = new("DisableHitTest");
    public static readonly PropertyKey<EventHandler<PointerEvent>> PointerDown = new("PointerDown");
    public static readonly PropertyKey<EventHandler<PointerEvent>> PointerMove = new("PointerMove");
    public static readonly PropertyKey<EventHandler<PointerEvent>> PointerUp = new("PointerUp");
    public static readonly PropertyKey<EventHandler<PointerEvent>> PreviewPointerDown = new("PreviewPointerDown");
    public static readonly PropertyKey<EventHandler<PointerEvent>> PreviewPointerMove = new("PreviewPointerMove");
    public static readonly PropertyKey<EventHandler<PointerEvent>> PreviewPointerUp = new("PreviewPointerUp");
    public static readonly PropertyKey<EventHandler> PointerCaptureLost = new("PointerCaptureLost");

    // Label, Window, Button, Checkbox, etc.
    public static readonly PropertyKey<string> Text = new("Text");
    public static readonly PropertyKey<string> FontName = new("FontName");
    public static readonly PropertyKey<double> FontSize = new("FontSize");

    // Borders, text, misc
    public static readonly PropertyKey<Color> Background = new("Background");
    public static readonly PropertyKey<Thickness> Padding = new("Padding");
    public static readonly PropertyKey<Color> BorderColor = new("BorderColor");
    public static readonly PropertyKey<double> BorderWidth = new("BorderWidth");
    public static readonly PropertyKey<double> BorderRadius = new("BorderRadius");

    // Row, Column, DockPanel, etc.
    public static readonly PropertyKey<bool> Wrap = new("Wrap");
    public static readonly PropertyKey<Size> Spacing = new("Spacing");
    public static readonly PropertyKey<Dock> Dock = new("Dock");

}
