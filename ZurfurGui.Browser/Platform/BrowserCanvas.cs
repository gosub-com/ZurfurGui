using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Base;
using ZurfurGui.Platform;

namespace ZurfurGui.Browser.Interop;

internal partial class BrowserCanvas : OsCanvas
{
    [JSImport("globalThis.ZurfurGui.getContext")]
    private static partial JSObject? GetContext(JSObject canvas, string contextId);

    [JSImport("globalThis.ZurfurGui.getBoundingClientRect")]
    private static partial JSObject GetBoundingClientRect(JSObject canvas);

    [JSImport("globalThis.ZurfurGui.observeCanvasInput")]
    private static partial JSObject ObserveCanvasInput(JSObject canvas, 
        [JSMarshalAs<JSType.Function<JSType.Object>>] Action<JSObject> callBack);

    [JSImport("globalThis.ZurfurGui.canvasHasFocus")]
    private static partial bool CanvasHasFocus(JSObject canvas);

    /// <summary>
    /// Stores canvas size (pixels) in canvas.devicePixelWidth and canvas.devicePixelHeight whenever size changes
    /// </summary>
    [JSImport("globalThis.ZurfurGui.observeCanvasDevicePixelSize")]
    private static partial JSObject ObserveCanvasDevicePixelSize(JSObject canvas);


    BrowserWindow _window;
    JSObject _canvas { get; set; }
    string _canvasId { get; set; }
    Action<PointerEvent>? _pointerInput;

    public OsContext Context { get; private set; }
    

    public BrowserCanvas(string canvasId, BrowserWindow window)
    {
        _window = window;
        _canvasId = canvasId;
        _canvas = BrowserWindow.GetElementById(canvasId)
                    ?? throw new Exception($"Expecting canvas DOM element with ID '{canvasId}'");
        Context = new BrowserContext(GetContext(_canvas, "2d")
            ?? throw new Exception($"Can't get context for '{canvasId}'"));

        // Observe input device
        ObserveCanvasInput(_canvas, ObserveCanvasInputEvents);

        // Observe physical device pixel size
        try
        {
            _canvas.SetProperty("devicePixelWidth", -1);
            _canvas.SetProperty("devicePixelHeight", -1);
            ObserveCanvasDevicePixelSize(_canvas);
        }
        catch
        {
            // Not supported
        }
    }

    void ObserveCanvasInputEvents(JSObject e)
    {
        var etype = e.GetPropertyAsString("type");

        switch (etype)
        {
            case "pointerenter":
            case "pointermove":
            case "pointerleave":
            case "pointerdown":
            case "pointerup":
                var canvasRect = GetBoundingClientRect(_canvas);
                var canvasPoint = new Point(canvasRect.GetPropertyAsDouble("x"), canvasRect.GetPropertyAsDouble("y"));
                var position = new Point(e.GetPropertyAsDouble("clientX"), e.GetPropertyAsDouble("clientY")) - canvasPoint;
                _pointerInput?.Invoke(new PointerEvent(etype, position.ToPoint * _window.DevicePixelRatio));
                break;
            default: 
                Console.WriteLine($"Un-processed event: {etype}");
                break;
        };
    }

    public bool HasFocus => CanvasHasFocus(_canvas);

    public Size DeviceSize
    {
        get => new Size(_canvas.GetPropertyAsDouble("width"), _canvas.GetPropertyAsDouble("height"));
        set { _canvas.SetProperty("width", value.Width); _canvas.SetProperty("height", value.Height); }
    }

    public Size StyleSize
    {
        get
        {
            var r = GetBoundingClientRect(_canvas);
            return new Size(r.GetPropertyAsDouble("width"), r.GetPropertyAsDouble("height"));
        }

        set
        {
            var style = _canvas.GetPropertyAsJSObject("style");
            if (style == null)
                throw new Exception("SetStyleSize: style was null");
            style.SetProperty("width", $"{value.Width}px");
            style.SetProperty("height", $"{value.Height}px");
        }
    }

    /// <summary>
    /// This is null until until ObserveDevicePixelSize is called, and the frame is drawn, and the browser supports it.
    /// </summary>
    public Size? DevicePixelSize
    {
        get
        {
            try
            {
                var width = _canvas.GetPropertyAsDouble("devicePixelWidth");
                var height = _canvas.GetPropertyAsDouble("devicePixelHeight");
                if (width >= 0 && height >= 0)
                    return new(width, height);
            }
            catch
            {
                // Not supported
            }
            return null;
        }
    }

    public Action<PointerEvent>? PointerInput 
    { 
        get => _pointerInput; 
        set { _pointerInput = value; } 
    }
}

