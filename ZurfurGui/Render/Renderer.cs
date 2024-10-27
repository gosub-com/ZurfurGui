
using System.Diagnostics;
using ZurfurGui.Controls;
using ZurfurGui.Platform;

namespace ZurfurGui.Render;


public class Renderer
{
    readonly bool CLIP_CONTROLS = true;

    OsWindow _window;
    View _view;

    int _frameCount;
    int _frameLastCountSecond;
    int _fps;
    int _second;
    long _totalMs;
    long _avgMs;

    double _devicePixelRatio = double.NaN;
    Size _mainWindowSize = new(double.NaN, double.NaN);


    public Renderer(OsWindow window, IEnumerable<Controllable> controls)
    {
        var mainWindow = new Canvas()
        {
            AlignHorizontal = HorizontalAlignment.Stretch,
            AlignVertical = VerticalAlignment.Stretch,
            Controls = controls.ToList()
        };

        _window = window;
        _view = View.BuildViewTree(mainWindow);
    }

    public void RenderFrame()
    {
        var timer = Stopwatch.StartNew();

        if (_mainWindowSize != _window.PrimaryCanvas.DeviceSize || _devicePixelRatio != _window.DevicePixelRatio)
        {
            _mainWindowSize = _window.PrimaryCanvas.DeviceSize / _window.DevicePixelRatio;
            _devicePixelRatio = _window.DevicePixelRatio;
            _view.Measure(_mainWindowSize, new MeasureContext(_window.PrimaryCanvas.Context));
            _view.Arrange(new Rect(new(0,0), _mainWindowSize));

        }

        _frameCount++;
        var now = DateTime.UtcNow;
        if (now.Second != _second)
        {
            _second = now.Second;
            _fps = _frameCount - _frameLastCountSecond;
            _frameLastCountSecond = _frameCount;
            if (_fps != 0)
                _avgMs = _totalMs / _fps;
            _totalMs = 0;
        }


        var context = _window.PrimaryCanvas.Context;
        var renderContext = new RenderContext(context);
        renderContext.PushOrigin(new(0, 0), _window.DevicePixelRatio);

        var left = 15;
        var top = 50;
        var ls = 26;
        RenderBackroundTest(renderContext, _window.PrimaryCanvas, left, top, ls);


        RenderView(renderContext, _view, new Rect(0, 0, 10000, 10000));

        var ms = timer.ElapsedMilliseconds;
        _totalMs += ms;
        if (CLIP_CONTROLS)
            renderContext.ClipRect(0, 0, 10000, 10000);

        renderContext.FontName = "sans-serif";
        renderContext.FontSize = 26;
        renderContext.FillColor = new Color(0xC0, 0xC0, 0xF0);
        renderContext.FillText($"|fps={_fps}, ms={_avgMs}, count={_frameCount}, {ms,-2}", left, top + 0 * ls);
    }

    private void RenderBackroundTest(RenderContext renderContext, OsCanvas canvas, int left, int top, int ls)
    {
        var context = canvas.Context;
        var canvasDeviceSize = canvas.DeviceSize;
        var canvasStyleSize = canvas.StyleSize;

        renderContext.FillColor = new Color(0x20, 0x20, 0x80);
        renderContext.FillRect(0, 0, canvasStyleSize.Width, canvasStyleSize.Height);

        renderContext.FillColor = new Color(0x80, 0x80, 0xF0);
        renderContext.FontName = "sans-serif";
        renderContext.FontSize = 16;

        for (var i = 0; i < 30; i++)
        {
            for (var j = 0; j < 30; j++)
            {
                var x = i * 50;
                var y = j * 20;
                renderContext.FillText($"{x / 10},{y / 10}", x, y);
            }
        }

        renderContext.FontName = "sans-serif";
        renderContext.FontSize = 500;
        renderContext.FillText($"M.M.", left, top + 17 * ls);

        renderContext.FontName = "sans-serif";
        renderContext.FontSize = 26;
        renderContext.FillColor = new Color(0xC0, 0xC0, 0xF0);

        renderContext.FillText($"|WPR={_window.DevicePixelRatio:F2}", left, top + 1 * ls);
        renderContext.FillText($"|CDS={canvasDeviceSize:F2}, CSS={canvasStyleSize:F2}", left, top + 2 * ls);

        renderContext.FillText($"│WIS={_window.InnerSize}, COS={canvas.DevicePixelSize?.ToString() ?? "?"}", left, top + 3 * ls);
        renderContext.FillText($"│Screen size: ({_window.ScreenSize})", left, top + 4 * ls);

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
    }

    int ViewLevel(View view)
    {
        int level = 0;
        while (view.ParentView != null)
        {
            level++;
            view = view.ParentView;
        }
        return level;
    }

    void RenderView(RenderContext renderContext, View view, Rect clip)
    {
        var bounds = view.Bounds;

        clip = clip.Intersect(bounds);
        clip = new Rect(clip.Position - bounds.Position, clip.Size);


        // Skip drawing when fully clipped
        if (clip.Width <= 0 || clip.Height <= 0)
            return;

        renderContext.PushOrigin(bounds.Position, 1);

        if (CLIP_CONTROLS)
            renderContext.ClipRect(clip.X, clip.Y, clip.Width, clip.Height);


        view.Control?.Render(renderContext);

        foreach (var child in view.Views)
        {
            RenderView(renderContext, child, clip);
        }



        renderContext.PopOrigin();

    }

}
