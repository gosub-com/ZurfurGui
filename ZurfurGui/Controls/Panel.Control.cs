using ZurfurGui.Base;
using ZurfurGui.Platform;
using ZurfurGui.Property;

namespace ZurfurGui.Controls;

public partial class Panel : Controllable
{
    // Basic panel functionality
    public static readonly PropertyKey<string> Name = new(".name", typeof(Panel), "");
    public static readonly PropertyKey<string> Controller = new(".controller", typeof(Panel), "");
    public static readonly PropertyKey<string> Namespace = new(".namespace", typeof(Panel), "");
    public static readonly PropertyKey<TextLines> Use = new(".use", typeof(Panel), new());
    public static readonly PropertyKey<string> Layout = new(".layout", typeof(Panel), "");
    public static readonly PropertyKey<Properties[]> Content = new(".content", typeof(Panel), Array.Empty<Properties>(), ViewFlags.ReMeasure);
    public static readonly PropertyKey<Dictionary<string, DataBinding>> Data = new(".data", typeof(Panel), new(), ViewFlags.ReMeasure);

    public static readonly PropertyKey<bool> IsVisible = new(".isVisible", typeof(Panel), true, ViewFlags.ReMeasure);
    public static readonly PropertyKeyMerge<AlignProp> Align = new(".align", typeof(Panel), new(), ViewFlags.ReMeasure);
    public static readonly PropertyKeyMerge<ThicknessProp> Margin = new(".margin", typeof(Panel), new(), ViewFlags.ReMeasure);
    public static readonly PropertyKeyMerge<SizeProp> SizeRequest = new(".sizeRequest", typeof(Panel), new(), ViewFlags.ReMeasure);
    public static readonly PropertyKeyMerge<SizeProp> SizeMax = new(".sizeMax", typeof(Panel), new(), ViewFlags.ReMeasure);
    public static readonly PropertyKeyMerge<SizeProp> SizeMin = new(".sizeMin", typeof(Panel), new(), ViewFlags.ReMeasure);
    public static readonly PropertyKeyMerge<DoubleProp> Magnification = new(".magnification", typeof(Panel), new(), ViewFlags.ReMeasure);
    public static readonly PropertyKeyMerge<EnumProp<bool>> Clip = new(".clip", typeof(Panel), new(), ViewFlags.ReMeasure);
    public static readonly PropertyKeyMerge<PointProp> Offset = new(".offset", typeof(Panel), new(), ViewFlags.ReMeasure);
    public static readonly PropertyKeyMerge<ThicknessProp> Padding = new(".padding", typeof(Panel), new(), ViewFlags.ReMeasure);
    public static readonly PropertyKeyMerge<ColorProp> BackgroundColor = new(".backgroundColor", typeof(Panel), new(), ViewFlags.ReDraw);
    public static readonly PropertyKeyMerge<ColorProp> BorderColor = new(".borderColor", typeof(Panel), new(), ViewFlags.ReDraw);
    public static readonly PropertyKeyMerge<DoubleProp> BorderWidth = new(".borderWidth", typeof(Panel), new(), ViewFlags.ReMeasure);
    public static readonly PropertyKeyMerge<DoubleProp> BorderRadius = new(".borderRadius", typeof(Panel), new(), ViewFlags.ReDraw);

    // Style
    public static readonly PropertyKey<TextLines> Selectors = new(".selectors", typeof(Panel), new(), ViewFlags.ReStyleDown);
    public static readonly PropertyKey<TextLines> Classes = new(".classes", typeof(Panel), new(), ViewFlags.ReStyleDown);
    public static readonly PropertyKey<TextLines> UseStyles = new(".useStyles", typeof(Panel), new(), ViewFlags.ReStyleDown);
    public static readonly PropertyKey<Properties[]> Styles = new(".styles", typeof(Panel), Array.Empty<Properties>(), ViewFlags.ReStyleDown);

    // Pseudo classes
    public static readonly PropertyKeyMerge<EnumProp<bool>> IsPointerOver = new(".isPointerOver", typeof(Panel), new(), ViewFlags.ReStyleThis);
    public static readonly PropertyKeyMerge<EnumProp<bool>> IsEnabled = new(".isEnabled", typeof(Panel), new(), ViewFlags.ReStyleDown);
    public static readonly PropertyKeyMerge<EnumProp<bool>> IsWindowActive = new(".isWindowInactive", typeof(Panel), new(), ViewFlags.ReStyleDown);
    public static readonly PropertyKeyMerge<EnumProp<bool>> IsDarkMode = new(".isDarkMode", typeof(Panel), new(), ViewFlags.ReStyleDown);
    public static readonly PropertyKeyMerge<EnumProp<bool>> IsPressed = new(".isPressed", typeof(Panel), new(), ViewFlags.ReStyleThis);
    public static readonly PropertyKeyMerge<EnumProp<bool>> IsFocused = new(".isFocused", typeof(Panel), new(), ViewFlags.ReStyleThis);
    public static readonly PropertyKeyMerge<EnumProp<bool>> IsFocusWithin = new(".isFocusWithin", typeof(Panel), new(), ViewFlags.ReStyleThis);

    // Common UI interaction
    public static readonly PropertyKeyMerge<EnumProp<bool>> DisableHitTest = new(".disableHitTest", typeof(Panel), new());
    public static readonly PropertyKey<EventHandler<PointerEvent>> PointerDown = new(".pointerDown", typeof(Panel), static (s, e) => { });
    public static readonly PropertyKey<EventHandler<PointerEvent>> PointerMove = new(".pointerMove", typeof(Panel), static (s, e) => { });
    public static readonly PropertyKey<EventHandler<PointerEvent>> PointerUp = new(".pointerUp", typeof(Panel), static (s, e) => { });
    public static readonly PropertyKey<EventHandler<PointerEvent>> PointerClick = new(".pointerClick", typeof(Panel), static (s, e) => { });
    public static readonly PropertyKey<EventHandler<PointerEvent>> PreviewPointerDown = new(".previewPointerDown", typeof(Panel), static (s, e) => { });
    public static readonly PropertyKey<EventHandler<PointerEvent>> PreviewPointerMove = new(".previewPointerMove", typeof(Panel), static (s, e) => { });
    public static readonly PropertyKey<EventHandler<PointerEvent>> PreviewPointerUp = new(".previewPointerUp", typeof(Panel), static (s, e) => { });
    public static readonly PropertyKey<EventHandler<PointerEvent>> PreviewPointerClick = new(".previewPointerClick", typeof(Panel), static (s, e) => { });
    public static readonly PropertyKey<EventHandler> PointerCaptureLost = new(".pointerCaptureLost", typeof(Panel), static (s, e) => { });


    public Panel()
    {
        InitializeControl();
    }

}
