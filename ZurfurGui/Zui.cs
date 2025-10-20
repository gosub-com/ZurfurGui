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
    public static readonly PropertyKey<Properties[]> Content = new("Content", ViewFlags.ReMeasure);

    // Basic View
    public static readonly PropertyKey<EnumProp<bool>> IsVisible = new("IsVisible", ViewFlags.ReMeasure);
    public static readonly PropertyKey<AlignProp> Align = new("Align", ViewFlags.ReMeasure);
    public static readonly PropertyKey<ThicknessProp> Margin = new("Margin", ViewFlags.ReMeasure);
    public static readonly PropertyKey<SizeProp> SizeRequest = new("SizeRequest", ViewFlags.ReMeasure);
    public static readonly PropertyKey<SizeProp> SizeMax = new("SizeMax", ViewFlags.ReMeasure);
    public static readonly PropertyKey<SizeProp> SizeMin = new("SizeMin", ViewFlags.ReMeasure);
    public static readonly PropertyKey<DoubleProp> Magnification = new("Magnification", ViewFlags.ReMeasure);
    public static readonly PropertyKey<EnumProp<bool>> Clip = new("Clip", ViewFlags.ReMeasure);
    public static readonly PropertyKey<PointProp> Offset = new("Offset", ViewFlags.ReMeasure);
    public static readonly PropertyKey<ThicknessProp> Padding = new("Padding", ViewFlags.ReMeasure);
    public static readonly PropertyKey<BackgroundProp> Background = new("Background", ViewFlags.ReMeasure);

    // Style
    public static readonly PropertyKey<TextLines> Selectors = new("Selectors", ViewFlags.ReStyle);
    public static readonly PropertyKey<TextLines> Classes = new("Classes", ViewFlags.ReStyle);
    public static readonly PropertyKey<Properties[]> Styles = new("Styles", ViewFlags.ReStyle);

    // Pseudo classes
    public static readonly PropertyKey<EnumProp<bool>> IsPointerOver = new("IsPointerOver", ViewFlags.RePseudo);
    public static readonly PropertyKey<EnumProp<bool>> IsEnabled = new("IsEnabled", ViewFlags.RePseudo);
    public static readonly PropertyKey<EnumProp<bool>> IsWindowActive = new("IsWindowInactive", ViewFlags.RePseudo);
    public static readonly PropertyKey<EnumProp<bool>> IsPressed = new("IsPressed", ViewFlags.RePseudo);
    public static readonly PropertyKey<EnumProp<bool>> IsFocused = new("IsFocused", ViewFlags.RePseudo);
    public static readonly PropertyKey<EnumProp<bool>> IsFocusWithin = new("IsFocusWithin", ViewFlags.RePseudo);

    // Label, Window, Button, Checkbox, etc.
    public static readonly PropertyKey<TextLinesProp> Text = new("Text", ViewFlags.ReMeasure);
    public static readonly PropertyKey<FontProp> Font = new("Font", ViewFlags.ReMeasure);
    public static readonly PropertyKey<ColorProp> Color = new("Color", ViewFlags.ReDraw);

    // Row, Column, DockPanel, etc.
    public static readonly PropertyKey<EnumProp<bool>> Wrap = new("Wrap", ViewFlags.ReMeasure);
    public static readonly PropertyKey<SizeProp> Spacing = new("Spacing", ViewFlags.ReMeasure);
    public static readonly PropertyKey<EnumProp<Dock>> Dock = new("Dock", ViewFlags.ReMeasure);

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
