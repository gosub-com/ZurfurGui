using ZurfurGui.Base;
using ZurfurGui.Layout;
using ZurfurGui.Platform;
using ZurfurGui.Property;
using ZurfurGui.Windows;

namespace ZurfurGui;

/// <summary>
/// Common Zurfur GUI constants
/// </summary>
public static class Zui
{
    // Basic View functionality
    public static readonly PropertyKey<string> Name = new("(Name)");
    public static readonly PropertyKey<string> Controller = new("Controller");
    public static readonly PropertyKey<string> Layout = new("Layout");
    public static readonly PropertyKey<Properties[]> Content = new("Content");

    // Basic View Style
    public static readonly PropertyKey<EnumProp<bool>> IsVisible = new("IsVisible");
    public static readonly PropertyKey<AlignProp> Align = new("Align");
    public static readonly PropertyKey<ThicknessProp> Margin = new("Margin");
    public static readonly PropertyKey<SizeProp> SizeRequest = new("SizeRequest");
    public static readonly PropertyKey<SizeProp> SizeMax = new("SizeMax");
    public static readonly PropertyKey<SizeProp> SizeMin = new("SizeMin");
    public static readonly PropertyKey<DoubleProp> Magnification = new("Magnification");
    public static readonly PropertyKey<EnumProp<bool>> Clip = new("Clip");
    public static readonly PropertyKey<PointProp> Offset = new("Offset");
    public static readonly PropertyKey<ThicknessProp> Padding = new("Padding");
    public static readonly PropertyKey<BackgroundProp> Background = new("Background");


    // Style
    public static readonly PropertyKey<TextLines> Selectors = new("Selectors");
    public static readonly PropertyKey<TextLines> Classes = new("Classes");
    public static readonly PropertyKey<Properties[]> Styles = new("Styles");

    // Label, Window, Button, Checkbox, etc.
    public static readonly PropertyKey<TextLinesProp> Text = new("Text");
    public static readonly PropertyKey<FontProp> Font = new("Font");

    // Row, Column, DockPanel, etc.
    public static readonly PropertyKey<EnumProp<bool>> Wrap = new("Wrap");
    public static readonly PropertyKey<SizeProp> Spacing = new("Spacing");
    public static readonly PropertyKey<EnumProp<Dock>> Dock = new("Dock");

    // Common UI interaction
    public static readonly PropertyKey<EnumProp<bool>> DisableHitTest = new("DisableHitTest");
    public static readonly PropertyKey<EventHandler<PointerEvent>> PointerDown = new("PointerDown");
    public static readonly PropertyKey<EventHandler<PointerEvent>> PointerMove = new("PointerMove");
    public static readonly PropertyKey<EventHandler<PointerEvent>> PointerUp = new("PointerUp");
    public static readonly PropertyKey<EventHandler<PointerEvent>> PreviewPointerDown = new("PreviewPointerDown");
    public static readonly PropertyKey<EventHandler<PointerEvent>> PreviewPointerMove = new("PreviewPointerMove");
    public static readonly PropertyKey<EventHandler<PointerEvent>> PreviewPointerUp = new("PreviewPointerUp");
    public static readonly PropertyKey<EventHandler> PointerCaptureLost = new("PointerCaptureLost");

}
