using ZurfurGui.Base;
using ZurfurGui.Controls;
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
    public static readonly PropertyKey<string> Name = new("Name", "");
    public static readonly PropertyKey<string> Controller = new("Controller", "");
    public static readonly PropertyKey<string> Layout = new("Layout", "");
    public static readonly PropertyKey<string> Draw = new("Draw", "");
    public static readonly PropertyKey<Properties[]> Content = new("Content", []);

    // Basic View Style
    public static readonly PropertyKey<bool> IsVisible = new("IsVisible", true);
    public static readonly PropertyKey<AlignHorizontal> AlignHorizontal = new("AlignHorizontal", Controls.AlignHorizontal.Stretch);
    public static readonly PropertyKey<AlignVertical> AlignVertical = new("AlignVertical", Controls.AlignVertical.Stretch);
    public static readonly PropertyKey<ThicknessProp> Margin = new("Margin", null);
    public static readonly PropertyKey<SizeProp> SizeRequest = new("SizeRequest", null);
    public static readonly PropertyKey<SizeProp> SizeMax = new("SizeMax", null);
    public static readonly PropertyKey<SizeProp> SizeMin = new("SizeMin", null);
    public static readonly PropertyKey<DoubleProp> Magnification = new("Magnification", 1);
    public static readonly PropertyKey<bool> Clip = new("Clip", false);
    public static readonly PropertyKey<PointProp> Offset = new("Offset", null);
    public static readonly PropertyKey<ThicknessProp> Padding = new("Padding", null);
    public static readonly PropertyKey<ColorProp> Background = new("Background", null);
    public static readonly PropertyKey<ColorProp> BorderColor = new("BorderColor", null);
    public static readonly PropertyKey<DoubleProp> BorderWidth = new("BorderWidth", null);
    public static readonly PropertyKey<DoubleProp> BorderRadius = new("BorderRadius", null);

    // Style
    public static readonly PropertyKey<TextLines> Classes = new("Classes", []);
    public static readonly PropertyKey<StyleSheet> DefaultStyle = new("DefaultStyle", new());
    public static readonly PropertyKey<TextLines> UseStyle = new("UseStyle", []);

    // Label, Window, Button, Checkbox, etc.
    public static readonly PropertyKey<TextLines> Text = new("Text", ["�"]);
    public static readonly PropertyKey<string> FontName = new("FontName", "Arial");
    public static readonly PropertyKey<DoubleProp> FontSize = new("FontSize", 16.0);

    // Row, Column, DockPanel, etc.
    public static readonly PropertyKey<bool> Wrap = new("Wrap", true);
    public static readonly PropertyKey<SizeProp> Spacing = new("Spacing", new(5,5));
    public static readonly PropertyKey<Dock> Dock = new("Dock", ZurfurGui.Layout.Dock.Left);

    // Common UI interaction
    public static readonly PropertyKey<bool> DisableHitTest = new("DisableHitTest", false);
    public static readonly PropertyKey<EventHandler<PointerEvent>> PointerDown = new("PointerDown", null!);
    public static readonly PropertyKey<EventHandler<PointerEvent>> PointerMove = new("PointerMove", null!);
    public static readonly PropertyKey<EventHandler<PointerEvent>> PointerUp = new("PointerUp", null!);
    public static readonly PropertyKey<EventHandler<PointerEvent>> PreviewPointerDown = new("PreviewPointerDown", null!);
    public static readonly PropertyKey<EventHandler<PointerEvent>> PreviewPointerMove = new("PreviewPointerMove", null!);
    public static readonly PropertyKey<EventHandler<PointerEvent>> PreviewPointerUp = new("PreviewPointerUp", null!);
    public static readonly PropertyKey<EventHandler> PointerCaptureLost = new("PointerCaptureLost", null!);

}
