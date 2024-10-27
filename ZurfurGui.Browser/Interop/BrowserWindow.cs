using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Platform;

namespace ZurfurGui.Browser.Interop;

internal partial class BrowserWindow : OsWindow
{
    JSObject _js;

    [JSImport("globalThis.document.getElementById")]
    public static partial JSObject? GetElementById(string elementId);

    [JSImport("globalThis.ZurfurGui.getBrowserWindow")]
    private static partial JSObject GetBrowserWindow();
    
    [JSImport("globalThis.requestAnimationFrame")]
    public static partial void RequestAnimationFrame([JSMarshalAs<JSType.Function>] Action callback);


    public BrowserWindow(string canvasId)
    {
        _js = GetBrowserWindow();
        PrimaryCanvas = new BrowserCanvas(GetElementById(canvasId)
            ?? throw new Exception($"Expecting canvas DOM element with ID '{canvasId}'"), canvasId);
    }


    public OsCanvas PrimaryCanvas { get; private set; }


    public double DevicePixelRatio => _js.GetPropertyAsDouble("devicePixelRatio");

    /// <summary>
    /// Includes scroll bar
    /// </summary>
    public Size InnerSize
        => new Size(_js.GetPropertyAsDouble("innerWidth"), _js.GetPropertyAsDouble("innerHeight"));

    /// <summary>
    /// Excludes scroll bar
    /// </summary>
    public Size OuterSize
        => new Size(_js.GetPropertyAsDouble("outerWidth"), _js.GetPropertyAsDouble("outerHeight"));

    /// <summary>
    /// Device screen size
    /// </summary>
    public Size? ScreenSize
    {
        get
        {
            var s = _js.GetPropertyAsJSObject("screen");
            if (s == null)
                return null;
            return new Size(s.GetPropertyAsDouble("width"), s.GetPropertyAsDouble("height"));
        }
    }

    /// <summary>
    /// Avalable device screen size
    /// </summary>
    public Size? AvailScreenSize
    {
        get
        {
            var s = _js.GetPropertyAsJSObject("screen");
            if (s == null)
                return null;
            return new Size(s.GetPropertyAsDouble("availWidth"), s.GetPropertyAsDouble("availHeight"));
        }
    }
}

