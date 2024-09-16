using Microsoft.AspNetCore.Components.RenderTree;
using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using ZurfurGui.Controls;
using static ZurfurGui.Browser.OsBrowser;

namespace ZurfurGui;


public class Render
{
    int _frameCount;

    public void RenderFrame()
    {
        var canvas = PrimaryCanvas;
        var context = PrimaryContext;

        _frameCount++;
        var timer = Stopwatch.StartNew();
        var px = PrimaryWindow.DevicePixelRatio;


        // Canvas size scales by device pixels
        // NOTE: We get high resolution, but css pixels (and font sizes) no longer scale
        var canvasBound = canvas.GetBoundingClientRect();
        canvas.Size = new Size(canvasBound.Width * px, canvasBound.Height * px);


        var canvasSize = canvas.Size;

        context.FillColor = new Color(0x20, 0x20, 0x80); 
        context.FillRect(0, 0, canvasSize.Width, canvasSize.Height);

        context.FillColor = new Color(0x80, 0x80, 0xF0);
        context.Font = $"{16 * px}px sans-serif";
        for (var i = 0; i < 30; i++)
        {
            for (var j = 0; j < 30; j++)
            {
                var x = i * 50;
                var y = j * 20;
                context.FillText($"{x / 10},{y / 10}", x * px, y * px);
            }
        }

        var left = 15 * px;
        var top = 50 * px;
        var ls = 26 * px;
        context.Font = $"{26 * px}px sans-serif";
        context.FillColor = new Color(0xC0, 0xC0, 0xF0);
        context.FillText($"|Count = {_frameCount}", left, top + 0*ls);

        context.FillText($"|Window pixel ratio={px}", left, top + 1*ls);
        context.FillText($"|Canvas size: ({canvasSize.Width},{canvasSize.Height})", left, top + 2*ls);

        context.FillText($"│Canvas bound size: ({canvasBound.Size})", left, top + 3*ls);
        context.FillText($"│Window inner size: ({PrimaryWindow.InnerSize})", left, top + 4*ls);
        context.FillText($"│Screen size: ({PrimaryWindow.ScreenSize})", left, top +5*ls);

        context.FillText($"Time: {timer.ElapsedMilliseconds} ms", left, top + 10*ls);

        context.FillText($"Text base: {context.Js.GetPropertyAsString("textBaseline")}", left, top + 11 * ls);

        var tm1 = context.MeasureText("M");
        var tm2 = context.MeasureText("a");

        FakeButton(context, px, "GenerateMyj", left, top + 6*ls);

        //context.Js.SetProperty("textBaseline", "top");
        FakeButton(context, px, "Hello", left, top + 8 * ls);
        context.FillText("Top", (int)(left +5*px), (int)(top + 8*ls));

        context.Js.SetProperty("textBaseline", "alphabetic");


        context.FillColor = new Color(255, 255, 255);
        context.Font = $"{12*px}px sans-serif";
        context.FillText($"M: {tm1}", left, top + 12 * ls);
        context.FillText($"a: {tm2}", left, top + 13 * ls);

        context.Font = $"10px sans-serif";
        var tm3 = context.MeasureText("M");
        context.Font = $"{12 * px}px sans-serif";
        context.FillText($"M10: {tm3}", left, top + 14 * ls);

        context.Font = $"100px sans-serif";
        var tm4 = context.MeasureText("M");
        context.Font = $"{12 * px}px sans-serif";
        context.FillText($"M100: {tm4}", left, top + 15 * ls);


        context.Font = $"100px sans-serif";
        context.Js.SetProperty("textBaseline", "top");
        var tm5 = context.MeasureText("M");
        context.Font = $"{12 * px}px sans-serif";
        context.FillText($"M100: {tm5}", left, top + 16 * ls);

        context.Js.SetProperty("textBaseline", "alphabetic");

        context.Font = $"1000px sans-serif";
        var tm6 = context.MeasureText("M");
        context.Font = $"{12 * px}px sans-serif";
        context.FillText($"M100: {tm6}", left, top + 17 * ls);

        var left1 = 400 * px;
        var left2 = 600 * px;


        RealButtonTop(context, "Sans Topj", "sans-serif", 26 * px, left1, top + 6*ls);
        RealButtonTop(context, "Arial Topj", "Arial", 26 * px, left1, top + 8 * ls);
        RealButtonTop(context, "﴿█j A﴿│|", "Arial", 26 * px, left1, top + 10 * ls);

        RealButtonAlphabetic(context, "Sans Topj", "sans-serif", 26 * px, left2, top + 6 * ls);
        RealButtonAlphabetic(context, "Arial Topj", "Arial", 26 * px, left2, top + 8 * ls);
        RealButtonAlphabetic(context, "﴿█j A﴿│|", "Arial", 26 * px, left2, top + 10 * ls);

    }

    static void FakeButton(BrowserContext context, double px, string text, double x, double y)
    {
        var fontSize = 26.0;
        context.FillColor = new Color(0xE0, 0xE0, 0xE0);
        context.FillRect(x, y, 180 * px, fontSize* px);
        context.FillColor = new Color(0x10, 0x10, 0x10);
        context.Font = $"{fontSize * px}px Arial";
        context.FillText(text, x + 5 * px, y + fontSize * px);
    }

    static void RealButtonTop(BrowserContext context, string text, string font, double fontSize, double x, double y)
    {
        context.Js.SetProperty("textBaseline", "top");
        context.Font = $"{fontSize}px {font}";
        var m = context.MeasureText(text);
        context.FillColor = new Color(0xE0, 0xE0, 0xE0);
        context.FillRect(x, y, m.Width, fontSize);
        context.FillColor = new Color(0x10, 0x10, 0x10);
        context.FillText(text, x, y);
        context.Js.SetProperty("textBaseline", "alphabetic");
    }
    static void RealButtonAlphabetic(BrowserContext context, string text, string font, double fontSize, double x, double y)
    {
        context.Js.SetProperty("textBaseline", "alphabetic");
        context.Font = $"{fontSize}px {font}";
        var m = context.MeasureText(text);
        context.FillColor = new Color(0xE0, 0xE0, 0xE0);
        context.FillRect(x, y, m.Width, fontSize);
        context.FillColor = new Color(0x10, 0x10, 0x10);
        context.FillText(text, x, y + fontSize);
    }
}
