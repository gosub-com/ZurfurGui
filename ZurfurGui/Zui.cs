using ZurfurGui.Base;
using ZurfurGui.Layout;
using ZurfurGui.Platform;
using ZurfurGui.Property;
using ZurfurGui.Controls;

namespace ZurfurGui;

/// <summary>
/// Common Zurfur GUI constants
/// </summary>
public static class Zui
{
    // Basic panel functionality
    public static readonly PropertyKey<string> Name = new(".name", typeof(Panel));
    public static readonly PropertyKey<string> Controller = new(".controller", typeof(Panel));
    public static readonly PropertyKey<string> Namespace = new(".namespace", typeof(Panel));
    public static readonly PropertyKey<string> Layout = new(".layout", typeof(Panel));
    public static readonly PropertyKey<Properties[]> Content = new(".content", typeof(Panel), ViewFlags.ReMeasure);
    public static readonly PropertyKey<TextLines> Data = new(".data", typeof(Panel), ViewFlags.ReMeasure);

    public static readonly PropertyKey<EnumProp<bool>> IsVisible = new(".isVisible", typeof(Panel), ViewFlags.ReMeasure);
    public static readonly PropertyKey<AlignProp> Align = new(".align", typeof(Panel), ViewFlags.ReMeasure);
    public static readonly PropertyKey<ThicknessProp> Margin = new(".margin", typeof(Panel), ViewFlags.ReMeasure);
    public static readonly PropertyKey<SizeProp> SizeRequest = new(".sizeRequest", typeof(Panel), ViewFlags.ReMeasure);
    public static readonly PropertyKey<SizeProp> SizeMax = new(".sizeMax", typeof(Panel), ViewFlags.ReMeasure);
    public static readonly PropertyKey<SizeProp> SizeMin = new(".sizeMin", typeof(Panel), ViewFlags.ReMeasure);
    public static readonly PropertyKey<DoubleProp> Magnification = new(".magnification", typeof(Panel), ViewFlags.ReMeasure);
    public static readonly PropertyKey<EnumProp<bool>> Clip = new(".clip", typeof(Panel), ViewFlags.ReMeasure);
    public static readonly PropertyKey<PointProp> Offset = new(".offset", typeof(Panel), ViewFlags.ReMeasure);
    public static readonly PropertyKey<ThicknessProp> Padding = new(".padding", typeof(Panel), ViewFlags.ReMeasure);
    public static readonly PropertyKey<ColorProp> BackgroundColor = new(".backgroundColor", typeof(Panel), ViewFlags.ReDraw);
    public static readonly PropertyKey<ColorProp> BorderColor = new(".borderColor", typeof(Panel), ViewFlags.ReDraw);
    public static readonly PropertyKey<DoubleProp> BorderWidth = new(".borderWidth", typeof(Panel), ViewFlags.ReMeasure);
    public static readonly PropertyKey<DoubleProp> BorderRadius = new(".borderRadius", typeof(Panel), ViewFlags.ReDraw);

    // Style
    public static readonly PropertyKey<TextLines> Selectors = new(".selectors", typeof(Panel), ViewFlags.ReStyleDown);
    public static readonly PropertyKey<TextLines> Classes = new(".classes", typeof(Panel), ViewFlags.ReStyleDown);
    public static readonly PropertyKey<TextLines> UseStyles = new(".useStyles", typeof(Panel), ViewFlags.ReStyleDown);
    public static readonly PropertyKey<Properties[]> Styles = new(".styles", typeof(Panel), ViewFlags.ReStyleDown);

    // Pseudo classes
    public static readonly PropertyKey<EnumProp<bool>> IsPointerOver = new(".isPointerOver", typeof(Panel), ViewFlags.ReStyleThis);
    public static readonly PropertyKey<EnumProp<bool>> IsEnabled = new(".isEnabled", typeof(Panel), ViewFlags.ReStyleDown);
    public static readonly PropertyKey<EnumProp<bool>> IsWindowActive = new(".isWindowInactive", typeof(Panel), ViewFlags.ReStyleDown);
    public static readonly PropertyKey<EnumProp<bool>> IsDarkMode = new(".isDarkMode", typeof(Panel), ViewFlags.ReStyleDown);
    public static readonly PropertyKey<EnumProp<bool>> IsPressed = new(".isPressed", typeof(Panel), ViewFlags.ReStyleThis);
    public static readonly PropertyKey<EnumProp<bool>> IsFocused = new(".isFocused", typeof(Panel), ViewFlags.ReStyleThis);
    public static readonly PropertyKey<EnumProp<bool>> IsFocusWithin = new(".isFocusWithin", typeof(Panel), ViewFlags.ReStyleThis);

    // Row, Column, DockPanel, etc.
    public static readonly PropertyKey<EnumProp<bool>> Wrap = new("cell.wrap", typeof(Panel), ViewFlags.ReMeasure);
    public static readonly PropertyKey<SizeProp> Spacing = new("cell.spacing", typeof(Panel), ViewFlags.ReMeasure);
    public static readonly PropertyKey<EnumProp<Dock>> Dock = new("dock.align", typeof(Panel), ViewFlags.ReMeasure);

    // Common UI interaction
    public static readonly PropertyKey<EnumProp<bool>> DisableHitTest = new(".disableHitTest", typeof(Panel));
    public static readonly PropertyKey<EventHandler<PointerEvent>> PointerDown = new(".pointerDown", typeof(Panel));
    public static readonly PropertyKey<EventHandler<PointerEvent>> PointerMove = new(".pointerMove", typeof(Panel));
    public static readonly PropertyKey<EventHandler<PointerEvent>> PointerUp = new(".pointerUp", typeof(Panel));
    public static readonly PropertyKey<EventHandler<PointerEvent>> PointerClick = new(".pointerClick", typeof(Panel));
    public static readonly PropertyKey<EventHandler<PointerEvent>> PreviewPointerDown = new(".previewPointerDown", typeof(Panel));
    public static readonly PropertyKey<EventHandler<PointerEvent>> PreviewPointerMove = new(".previewPointerMove", typeof(Panel));
    public static readonly PropertyKey<EventHandler<PointerEvent>> PreviewPointerUp = new(".previewPointerUp", typeof(Panel));
    public static readonly PropertyKey<EventHandler<PointerEvent>> PreviewPointerClick = new(".previewPointerClick", typeof(Panel));
    public static readonly PropertyKey<EventHandler> PointerCaptureLost = new(".pointerCaptureLost", typeof(Panel));

}
