using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Platform;

namespace ZurfurGui.Browser.Interop;

internal partial class BrowserCanvas : OsCanvas
{
    [JSImport("globalThis.ZurfurGui.getContext")]
    private static partial JSObject? GetContext(JSObject canvas, string contextId);

    [JSImport("globalThis.ZurfurGui.getBoundingClientRect")]
    private static partial JSObject GetBoundingClientRect(JSObject canvas);

    /// <summary>
    /// Stores canvas size (pixels) in canvas.devicePixelWidth and canvas.devicePixelHeight whenever size changes
    /// </summary>
    [JSImport("globalThis.ZurfurGui.observeCanvasDevicePixelSize")]
    private static partial JSObject ObserveCanvasDevicePixelSize(JSObject canvas);


    JSObject _canvas { get; set; }
    string _canvasId { get; set; }

    public OsContext Context { get; private set; }
    

    public BrowserCanvas(JSObject canvas, string canvasId)
    {
        _canvas = canvas;
        _canvasId = canvasId;
        Context = new BrowserContext(GetContext(_canvas, "2d")
            ?? throw new Exception($"Can't get context for '{canvasId}'"));

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

}

