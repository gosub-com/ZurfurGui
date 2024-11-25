
using System.Diagnostics;
using ZurfurGui.Controls;
using ZurfurGui.Platform;

namespace ZurfurGui.Render;


public class Renderer
{
    OsWindow _window;
    OsCanvas _canvas;
    View _view;
    BackgroundTest? _background;
    RenderContext _renderContext;

    View? _hoverView;

    int _frameCount;
    int _frameLastCountSecond;
    int _fps;
    int _second;
    long _totalMs;
    long _avgMs;

    double _devicePixelRatio = double.NaN;
    Size _mainWindowSize = new(double.NaN, double.NaN);

    Point _pointerPosition;


    public Renderer(OsWindow window, OsCanvas canvas, Properties controls)
    {
        const string BACKGROUND_NAME = "ZGui.BackgroundTest";

        _window = window;
        _canvas = canvas;
        _renderContext = new RenderContext(_canvas.Context);

        Properties background = [
            (ZGui.Controller, "ZGui.BackgroundTest"),
            (ZGui.Name, BACKGROUND_NAME)
        ];

        // Add canvas to top level controls, so we can adjust device pixel resolution
        controls = [
            (ZGui.Controller, "ZGui.Panel"),
            (ZGui.Controls, (Properties[])[background, controls])
        ];

        _view = Helper.BuildViewFromProperties(controls);

        var back = _view.FindAllByName(BACKGROUND_NAME);
        if (back.Count != 0)
            _background = back[0].Control as BackgroundTest;
        _background?.SetWindow(window, canvas);

        if (_canvas.PointerInput != null)
            throw new ArgumentException("Pointer input already taken", nameof(_canvas));
        _canvas.PointerInput = PointerInput;
    }

    void PointerInput(PointerEvent ev)
    {
        _pointerPosition = ev.Position;
        
        if (ev.Type == "pointermove")
        {
            var hit = View.FindHitTarget(_view, ev.Position);
            if (hit != null)
                Debug.WriteLine($"Hit: {hit}");
            if (hit != _hoverView)
            {
                if (_hoverView != null)
                    _hoverView.PointerHoverTarget = false;
                _hoverView = hit;
                if (_hoverView != null)
                    _hoverView.PointerHoverTarget = true;
            }
        }
    }


    public void RenderFrame()
    {
        var timer = Stopwatch.StartNew();

        if (_mainWindowSize != _canvas.DeviceSize || _devicePixelRatio != _window.DevicePixelRatio)
        {
            _view.Properties.Set(ZGui.Magnification, _window.DevicePixelRatio);
            _mainWindowSize = _canvas.DeviceSize / _window.DevicePixelRatio;
            _devicePixelRatio = _window.DevicePixelRatio;
            var measureConext = new MeasureContext(_canvas.Context);
            _view.Measure(_mainWindowSize, measureConext);
            _view.Arrange(new Rect(new(0,0), _mainWindowSize), measureConext);
            _view.PostArrange(new(), 1, new Rect(0, 0, 1000000, 1000000));
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

        _renderContext.SetPointerPosition(_pointerPosition);
        _background?.SetStats(_frameCount, _fps, (int)_avgMs);

        RenderView(_view);

        _totalMs += timer.ElapsedMilliseconds;
    }


    void RenderView(View view)
    {
        // Skip drawing when fully clipped
        var clip = view.Clip;
        if (clip.Width <= 0 || clip.Height <= 0)
            return;

        _renderContext.SetOrigin(view.Origin, view.Scale);
        _renderContext.ClipRect(clip.X, clip.Y, clip.Width, clip.Height);
        view.Control.Render(_renderContext);

        foreach (var child in view.Views)
            RenderView(child);
    }

}
