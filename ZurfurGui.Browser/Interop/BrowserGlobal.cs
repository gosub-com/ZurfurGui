//
// Browser specific platform stuff.
// Eventually, this should be the only file that needs to be ported for other platforms.
//


using System.Runtime.InteropServices.JavaScript;
using ZurfurGui.Platform;

namespace ZurfurGui.Browser.Interop;

internal partial class BrowserGlobal : OsGlobal
{

    [JSImport("globalThis.document.getElementById")]
    public static partial JSObject? GetElementById(string elementId);

    [JSImport("globalThis.ZurfurGui.window")]
    private static partial JSObject GetBrowserWindow();


    public BrowserGlobal(string canvasId)
    {
        PrimaryWindow = new BrowserWindow(GetBrowserWindow());
        PrimaryCanvas = new BrowserCanvas(GetElementById(canvasId)
            ?? throw new Exception($"Expecting canvas DOM element with ID '{canvasId}'"), canvasId);
    }


    public OsWindow PrimaryWindow { get; private set; }

    public OsCanvas PrimaryCanvas { get; private set; }
}

