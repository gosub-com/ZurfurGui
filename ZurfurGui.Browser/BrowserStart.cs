using Microsoft.JSInterop;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

using ZurfurGui.Browser.Interop;
using ZurfurGui.Controls;
using ZurfurGui.Render;

namespace ZurfurGui.Browser;

public static partial class BrowserStart
{
    const string STARTUP_CODE_NAME = "ZurfurGui.Browser.BrowserStart.js";

    [JSExport]
    public static string GetStartupScript()
    {
        return GetEmbeddedResourceString(STARTUP_CODE_NAME);
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

    public static void Start(string canvasId, Properties controls)
    {
        Initialize.Init();

        var window = new BrowserWindow(canvasId);
        var renderer = new Renderer(window, controls);
        var canvas = window.PrimaryCanvas;
        var context = canvas.Context;

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
            if (deviceSize  != null && deviceSize.Value.Width > 0 && deviceSize.Value.Height > 0 )
            {
                // Pixel perfect
                canvas.DeviceSize = deviceSize.Value;
            }
            else
            {
                // Close enough
                canvas.DeviceSize = px * canvas.StyleSize;
            }

            renderer.RenderFrame();
        }

    }
}