using Microsoft.JSInterop;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

using ZurfurGui.Browser.Interop;
using ZurfurGui.Controls;
using ZurfurGui.Render;

namespace ZurfurGui.Browser;

public static class BrowserStart
{

    public static void StartRendering(string canvasId, IEnumerable<Controllable> controls)
    {
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