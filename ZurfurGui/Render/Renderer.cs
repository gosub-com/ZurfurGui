
using System.Diagnostics;
using System.Runtime.InteropServices;
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
        const string BACKGROUND_NAME = "Zui.BackgroundTest";

        _window = window;
        _canvas = canvas;
        _renderContext = new RenderContext(_canvas.Context);

        Properties background = [
            (Zui.Controller, BACKGROUND_NAME),
            (Zui.Name, BACKGROUND_NAME)
        ];

        // Add canvas to top level controls, so we can adjust device pixel resolution
        controls = [
            (Zui.Controller, "Zui.Panel"),
            (Zui.Content, (Properties[])[background, controls])
        ];

        _view = Helper.BuildView(controls);

        _background = _view.FindByName(BACKGROUND_NAME).Control as BackgroundTest;
        _background?.SetWindow(window, canvas);

        if (_canvas.PointerInput != null)
            throw new ArgumentException("Pointer input already taken", nameof(_canvas));
        _canvas.PointerInput = PointerInput;
    }


    void PointerInput(PointerEvent ev)
    {
        _pointerPosition = ev.Position;
        var hit = View.FindHitTarget(_view, ev.Position);

        // Update hover target        
        if (ev.Type == "pointermove")
        {
            if (hit != _hoverView)
            {
                if (_hoverView != null)
                    _hoverView.PointerHoverTarget = false;
                _hoverView = hit;
                if (_hoverView != null)
                    _hoverView.PointerHoverTarget = true;
            }
        }

        var chain = new List<View>();
        GetViewChain(hit, chain);

        PropertyKey<Action<PointerEvent>> property;
        switch (ev.Type)
        {
            case "pointermove": property = Zui.PreviewPointerMove; break;
            case "pointerdown": property = Zui.PreviewPointerDown; break;
            case "pointerup": property = Zui.PreviewPointerUp; break;
            default: return;
        }

        // Preview
        for (int i = chain.Count - 1; i >= 0; i--)
        {
            var view = chain[i];
            var e = view.Properties.Get(property);
            if (e != null)
                e(ev);
        }

        switch (ev.Type)
        {
            case "pointermove": property = Zui.PointerMove; break;
            case "pointerdown": property = Zui.PointerDown; break;
            case "pointerup": property = Zui.PointerUp; break;
            default: return;
        }

        // Bubble
        foreach (var view in chain)
        {
            var e = view.Properties.Get(property);
            if (e != null)
                e(ev);
        }
    }

    /// <summary>
    /// Retrieve views from the given child up to the root
    /// </summary>
    void GetViewChain(View? view, List<View> views)
    {
        while (view != null)
        {
            views.Add(view);
            view = view.Parent;
        }
    }


    public void RenderFrame()
    {
        var timer = Stopwatch.StartNew();

        if (_mainWindowSize != _canvas.DeviceSize || _devicePixelRatio != _window.DevicePixelRatio)
        {
            _view.Properties.Set(Zui.Magnification, _window.DevicePixelRatio);
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
