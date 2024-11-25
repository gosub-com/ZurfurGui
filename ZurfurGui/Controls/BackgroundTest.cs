using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Platform;
using ZurfurGui.Render;

namespace ZurfurGui.Controls;

internal class BackgroundTest : Controllable
{
    readonly bool DRAW_NUMBERS = false;

    OsWindow? _window;
    OsCanvas? _canvas;

    int _frameCount;
    int _fps;
    long _avgMs;

    public string Type => "ZGui.BackgroundTest";
    public override string ToString() => View.ToString();
    public View View { get; private set; }

    public BackgroundTest()
    {
        View = new(this);
    }

    public View BuildView(Properties properties)
    {
        return View;
    }

    public void SetStats(int count, int fps, int avgMs)
    {
        _fps = fps;
        _avgMs = avgMs;
        _frameCount = count;
    }

    public void SetWindow(OsWindow window, OsCanvas canvas)
    {
        _window = window;
        _canvas = canvas;
    }

    public Size MeasureView(Size available, MeasureContext measure) => available;
    public Size ArrangeViews(Size final, MeasureContext measure) => final;

    public void Render(RenderContext renderContext) 
    {
        var left = 15;
        var top = 50;
        var ls = 26;

        renderContext.FillColor = new Color(0x20, 0x20, 0x80);
        renderContext.FillRect(0, 0, View.Size.Width, View.Size.Height);

        renderContext.FillColor = new Color(0x80, 0x80, 0xF0);
        renderContext.FontName = "sans-serif";
        renderContext.FontSize = 16;

        if (DRAW_NUMBERS)
        {
            for (var i = 0; i < 30; i++)
            {
                for (var j = 0; j < 30; j++)
                {
                    var x = i * 50;
                    var y = j * 20;
                    renderContext.FillText($"{x / 10},{y / 10}", x, y);
                }
            }
        }

        // Big "M.M"
        renderContext.FontName = "sans-serif";
        renderContext.FontSize = 500;
        renderContext.FillText($"M.M.", left, top + 17 * ls);

        // Show stats
        renderContext.FontName = "sans-serif";
        renderContext.FontSize = 26;
        renderContext.FillColor = new Color(0xC0, 0xC0, 0xF0);
        if (_window != null && _canvas != null)
        {
            var canvasDeviceSize = _canvas.DeviceSize;
            var canvasStyleSize = _canvas.StyleSize;
            renderContext.FillText($"|WPR={_window.DevicePixelRatio:F2}", left, top + 1 * ls);
            renderContext.FillText($"|CDS={canvasDeviceSize:F2}, CSS={canvasStyleSize:F2}", left, top + 2 * ls);
            renderContext.FillText($"│WIS={_window.InnerSize}, COS={_canvas.DevicePixelSize?.ToString() ?? "?"}", left, top + 3 * ls);
            renderContext.FillText($"│Screen size: ({_window.ScreenSize}), foucs={_canvas.HasFocus}", left, top + 4 * ls);
        }

        // Squares
        var sx = 500;
        renderContext.LineWidth = 1;
        renderContext.StrokeColor = Colors.Red;
        renderContext.StrokeRect(10 + sx, 10, 50, 50);

        renderContext.LineWidth = 2;
        renderContext.StrokeColor = Colors.Green;
        renderContext.StrokeRect(60 + sx, 60, 50, 50);

        renderContext.LineWidth = 3;
        renderContext.StrokeColor = Colors.Blue;
        renderContext.StrokeRect(110 + sx, 110, 50, 50);

        renderContext.LineWidth = 8;
        renderContext.StrokeColor = new Color(255, 255, 0);
        renderContext.StrokeRect(160 + sx, 60, 50, 50);

        renderContext.LineWidth = 16;
        renderContext.StrokeColor = new Color(255, 0, 255);
        renderContext.StrokeRect(210 + sx, 10, 50, 50);

        // More stats
        renderContext.FontName = "sans-serif";
        renderContext.FontSize = 26;
        renderContext.FillColor = new Color(0xC0, 0xC0, 0xF0);
        renderContext.FillText($"|fps={_fps}, ms={_avgMs}, count={_frameCount}", left, top + 0 * ls);
    }

}
