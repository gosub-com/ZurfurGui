using System.Reflection;
using System.Runtime.InteropServices.JavaScript;
using ZurfurGui.Browser.Interop;
using ZurfurGui.Render;
using ZurfurGui.Windows;

namespace ZurfurGui.Browser;

/// <summary>
/// Provides entry points for starting ZurfurGui in a browser host. See
/// <see href="../../docs/architecture.md">docs/architecture.md</see> for details about platform
/// hosting.
/// </summary>
public static partial class BrowserStart
{
    const string BROWSER_START_SCRIPT_NAME = "ZurfurGui.Browser.BrowserStart.js";

    /// <summary>
    /// Gets the embedded JavaScript module used to start the browser host.
    /// </summary>
    [JSExport]
    public static string GetBrowserStartScript()
    {
        return GetEmbeddedResourceString(BROWSER_START_SCRIPT_NAME);
    }

    private static string GetEmbeddedResourceString(string name)
    {
        try
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
            ArgumentNullException.ThrowIfNull(stream, $"Can't find resource file");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
        catch
        {
            throw new ArgumentException($"Error loading embedded resource '{name}'");
        }
    }

    /// <summary>
    /// Starts the GUI on the specified canvas and begins rendering frames.
    /// </summary>
    /// <param name="canvasId">The ID of the HTML canvas element to render into.</param>
    /// <param name="mainAppEntry">The callback used to initialize the application window.</param>
    public static void Start(string canvasId, Action<AppWindow> mainAppEntry)
    {
        var lastSize = new Base.Size(-1, -1);
        var appWindow = Loader.Init(mainAppEntry);
        var window = new BrowserWindow();
        var canvas = new BrowserCanvas(canvasId, window);
        var renderer = new Renderer(window, canvas, appWindow);

        // On first render, allow an exception to percolate up and show an error message
        ScaleAndRender();
        BrowserWindow.RequestAnimationFrame(RetryScaleAndRender);
        return;

        void RetryScaleAndRender()
        {
            try
            {
                ScaleAndRender();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error while rendering: {e.Message}");
            }
            BrowserWindow.RequestAnimationFrame(RetryScaleAndRender);
        }

        void ScaleAndRender()
        {
            canvas.StyleSize = window.InnerSize;

            // Canvas size scales by device pixels
            var px = window.DevicePixelRatio;
            var deviceSize = canvas.DevicePixelSize;
            Base.Size size;
            if (deviceSize != null && deviceSize.Value.Width > 0 && deviceSize.Value.Height > 0)
            {
                // Pixel perfect
                size = deviceSize.Value;
            }
            else
            {
                // Close enough
                size = px * canvas.StyleSize;
            }
            if (size != lastSize)
                canvas.DeviceSize = size;
            lastSize = size;

            renderer.RenderFrame();
        }

    }
}