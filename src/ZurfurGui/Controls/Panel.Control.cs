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
    public static readonly PropertyKey<string> Implements = new(".implements", typeof(Panel), "");
    public static readonly PropertyKey<Properties[]> Content = new(".content", typeof(Panel), Array.Empty<Properties>(), ViewFlags.ReMeasure);
    public static readonly PropertyKey<Dictionary<string, System.Text.Json.JsonElement>> DataProperties = new(".dataProperties", typeof(Panel), new(), ViewFlags.None);

    // NOTE: Data properties are used only by the loader, so don't exist at runtime.  This is just a placeholder.
    public static readonly PropertyKey<Dictionary<string, string>> Data = new(".data", typeof(Panel), new(), ViewFlags.ReMeasure);

    public static readonly PropertyKey<bool> IsVisible = new(".isVisible", typeof(Panel), true, ViewFlags.ReMeasure);
    public static readonly PropertyKey<AlignProp> Align = new(".align", typeof(Panel), new(), ViewFlags.ReMeasure);
    public static readonly PropertyKey<ThicknessProp> Margin = new(".margin", typeof(Panel), new(), ViewFlags.ReMeasure);
    public static readonly PropertyKey<SizeProp> SizeRequest = new(".sizeRequest", typeof(Panel), new(), ViewFlags.ReMeasure);
    public static readonly PropertyKey<SizeProp> SizeMax = new(".sizeMax", typeof(Panel), new(), ViewFlags.ReMeasure);
    public static readonly PropertyKey<SizeProp> SizeMin = new(".sizeMin", typeof(Panel), new(), ViewFlags.ReMeasure);
    public static readonly PropertyKey<double> Magnification = new(".magnification", typeof(Panel), 1, ViewFlags.ReMeasure);
    public static readonly PropertyKey<bool> Clip = new(".clip", typeof(Panel), false, ViewFlags.ReMeasure);
    public static readonly PropertyKey<PointProp> Offset = new(".offset", typeof(Panel), new(), ViewFlags.ReMeasure);
    public static readonly PropertyKey<ThicknessProp> Padding = new(".padding", typeof(Panel), new(), ViewFlags.ReMeasure);
    public static readonly PropertyKey<Color> BackgroundColor = new(".backgroundColor", typeof(Panel), new(), ViewFlags.ReDraw);
    public static readonly PropertyKey<Color> BorderColor = new(".borderColor", typeof(Panel), new(), ViewFlags.ReDraw);
    public static readonly PropertyKey<double> BorderWidth = new(".borderWidth", typeof(Panel), new(), ViewFlags.ReMeasure);
    public static readonly PropertyKey<double> BorderRadius = new(".borderRadius", typeof(Panel), new(), ViewFlags.ReDraw);

    // Style
    public static readonly PropertyKey<TextLines> Selectors = new(".selectors", typeof(Panel), new(), ViewFlags.ReStyleDown);
    public static readonly PropertyKey<TextLines> Classes = new(".classes", typeof(Panel), new(), ViewFlags.ReStyleDown);
    public static readonly PropertyKey<TextLines> UseStyles = new(".useStyles", typeof(Panel), new(), ViewFlags.ReStyleDown);
    public static readonly PropertyKey<Properties[]> Styles = new(".styles", typeof(Panel), Array.Empty<Properties>(), ViewFlags.ReStyleDown);

    // Pseudo classes
    public static readonly PropertyKey<bool> IsPointerOver = new(".isPointerOver", typeof(Panel), new(), ViewFlags.ReStyleThis);
    public static readonly PropertyKey<bool> IsEnabled = new(".isEnabled", typeof(Panel), new(), ViewFlags.ReStyleDown);
    public static readonly PropertyKey<bool> IsWindowActive = new(".isWindowInactive", typeof(Panel), new(), ViewFlags.ReStyleDown);
    public static readonly PropertyKey<bool> IsDarkMode = new(".isDarkMode", typeof(Panel), new(), ViewFlags.ReStyleDown);
    public static readonly PropertyKey<bool> IsPressed = new(".isPressed", typeof(Panel), new(), ViewFlags.ReStyleThis);
    public static readonly PropertyKey<bool> IsFocused = new(".isFocused", typeof(Panel), new(), ViewFlags.ReStyleThis);
    public static readonly PropertyKey<bool> IsFocusWithin = new(".isFocusWithin", typeof(Panel), new(), ViewFlags.ReStyleThis);

    // Common UI interaction
    public static readonly PropertyKey<bool> DisableHitTest = new(".disableHitTest", typeof(Panel), new());
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
