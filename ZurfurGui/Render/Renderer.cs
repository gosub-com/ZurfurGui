
using System.Diagnostics;

using ZurfurGui.Platform;

namespace ZurfurGui.Render;


public class Renderer
{
    int _frameCount;
    int _frameLastCountSecond;
    int _fps;
    int _second;

    public void RenderFrame(OsGlobal global)
    {
        var timer = Stopwatch.StartNew();

        _frameCount++;
        var now = DateTime.UtcNow;
        if (now.Second != _second)
        {
            _second = now.Second;
            _fps = _frameCount - _frameLastCountSecond;
            _frameLastCountSecond = _frameCount;
        }

        var window = global.PrimaryWindow;
        var canvas = global.PrimaryCanvas;
        var context = canvas.Context;


        var canvasDeviceSize = canvas.DeviceSize;
        var canvasClientSize = canvas.ClientSize;

        context.FillColor = new Color(0x20, 0x20, 0x80); 
        context.FillRect(0, 0, canvasClientSize.Width, canvasClientSize.Height);

        context.FillColor = new Color(0x80, 0x80, 0xF0);
        context.Font = "sans-serif";
        context.FontSize = 16;

        for (var i = 0; i < 30; i++)
        {
            for (var j = 0; j < 30; j++)
            {
                var x = i * 50;
                var y = j * 20;
                context.FillText($"{x / 10},{y / 10}", x, y);
            }
        }


        var left = 15;
        var top = 50;
        var ls = 26;

        context.Font = "sans-serif";
        context.FontSize = 500;
        context.FillText($"M.", left, top + 17 * ls);


        context.Font = "sans-serif";
        context.FontSize = 26;
        context.FillColor = new Color(0xC0, 0xC0, 0xF0);
        context.FillText($"|FPS={_fps}, Count = {_frameCount}", left, top + 0*ls);

        context.FillText($"|Window pixel ratio={window.DevicePixelRatio:F2}", left, top + 1*ls);
        context.FillText($"|Canvas device size: ({canvasDeviceSize:F2}), pixel scale: ({context.PixelScale:F2})", left, top + 2*ls);

        context.FillText($"│Canvas client size: ({canvasClientSize:F2})", left, top + 3*ls);
        context.FillText($"│Window inner size: ({window.InnerSize})", left, top + 4*ls);
        context.FillText($"│Screen size: ({window.ScreenSize})", left, top +5*ls);
        context.FillText($"Canvas on screen: ({canvas.DevicePixelSize?.ToString()??"?"})", left, top + 6*ls);

        context.FillText($"Time: {timer.ElapsedMilliseconds} ms", left, top + 10*ls);

        var left2 = 600;
        RealButtonAlphabetic(context, "Sans Topj", "sans-serif", 26, left2, top + 6 * ls);
        RealButtonAlphabetic(context, "Arial Topj", "Arial", 26, left2, top + 8 * ls);
        RealButtonAlphabetic(context, "﴿█j A﴿│|", "Arial", 26, left2, top + 10 * ls);

    }

    static void RealButtonAlphabetic(OsContext context, string text, string font, double fontSize, double x, double y)
    {
        context.Font = font;
        context.FontSize = fontSize;
        
        var textWidth = context.MeasureTextWidth(text);
        context.FillColor = new Color(0xE0, 0xE0, 0xE0);
        context.FillRect(x, y, textWidth, fontSize);
        context.FillColor = new Color(0x10, 0x10, 0x10);
        context.FillText(text, x, y + fontSize);
    }
}
