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

    public async static void StartRendering(string canvasId, Controllable control)
    {
        var window = new BrowserWindow(canvasId);
        var renderer = new Renderer(window, control);

        var canvas = window.PrimaryCanvas;
        var context = canvas.Context;

        ScaleAndRender();
        await Task.Delay(15);
        while (true)
        {
            // TBD: Hook browser screen refresh
            var wait = Task.Delay(15);
            try
            {
                ScaleAndRender();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error while rendering: {e.Message}");
            }
            await wait;
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