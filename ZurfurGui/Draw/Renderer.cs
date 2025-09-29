
using System.Diagnostics;
using ZurfurGui.Base;
using ZurfurGui.Platform;
using ZurfurGui.Property;
using ZurfurGui.Windows;

namespace ZurfurGui.Draw;

public class Renderer
{
    OsWindow _window;
    OsCanvas _canvas;
    AppWindow _appWindow;
    RenderContext _renderContext;

    View? _hoverView;

    long _frameCount;
    long _frameLastCountSecond;
    long _fps;
    int _second;
    long _totalMs;
    long _avgMs;

    double _devicePixelRatio = double.NaN;
    Size _mainWindowSize = new(double.NaN, double.NaN);

    Point _pointerPosition;

    public long Fps => _fps;
    public double AvgMs => _avgMs;
    public long FrameCount => _frameCount;
    public OsCanvas Canvas => _canvas;
    public OsWindow Window => _window;
    public RenderContext RenderContext => _renderContext;


    public Renderer(OsWindow window, OsCanvas canvas, AppWindow appWindow)
    {
        _window = window;
        _canvas = canvas;
        _renderContext = new RenderContext(_canvas.Context);

        _appWindow = appWindow;
        _appWindow.SetRenderWindow(this);

        if (_canvas.PointerInput != null)
            throw new ArgumentException("Pointer input already taken", nameof(_canvas));
        _canvas.PointerInput = PointerInput;
    }



    void PointerInput(PointerEvent ev)
    {
        _pointerPosition = ev.Position;

        // Perform capture
        if (_appWindow._pointerCaptureList.Count != 0)
        {
            // End hover target
            if (_hoverView != null)
                _hoverView.PointerHoverTarget = false;
            _hoverView = null;

            // Send events to captured views
            if (ev.Type == "pointerup" || ev.Type == "pointerleave")
                _appWindow.ClearPointerCaptureList();
            else
                SendPointerEvent(ev, _appWindow._pointerCaptureList);
        }

        // Update hover target        
        var hit = FindHitTarget(_appWindow.View, ev.Position);
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

        SendPointerEvent(ev, chain);

    }

    public static View? FindHitTarget(View view, Point target)
    {
        // Quick exit when not visible or not in clip region
        var clip = new Rect(view.Origin, view.toDevice(view.Size));
        if (!clip.Contains(target))
            return null;

        if (!view.GetStyle(Zui.IsVisible).Or(true))
            return null;

        // Check children first
        var views = view.Children;
        for (var i = views.Count - 1; i >= 0; i--)
        {
            var hit = FindHitTarget(views[i], target);
            if (hit != null)
                return hit;
        }

        if (!view.GetProperty(Zui.DisableHitTest).Or(false))
        {
            // User content hit test
            if (view.Draw is Drawable renderable)
                if (renderable.IsHit(view, target))
                    return view;

            // Panel hit test
            if (DrawHelper.IsHitPanel(view, target))
                return view;
        }

        return null;
    }



    private static void SendPointerEvent(PointerEvent ev, List<View> views)
    {
        PropertyKey<EventHandler<PointerEvent>> property;
        switch (ev.Type)
        {
            case "pointermove": property = Zui.PreviewPointerMove; break;
            case "pointerdown": property = Zui.PreviewPointerDown; break;
            case "pointerup": property = Zui.PreviewPointerUp; break;
            default: return;
        }

        // Preview
        for (int i = views.Count - 1; i >= 0; i--)
        {
            var view = views[i];
            var e = view.GetProperty(property);
            if (e != null)
                e(null, ev);
        }

        switch (ev.Type)
        {
            case "pointermove": property = Zui.PointerMove; break;
            case "pointerdown": property = Zui.PointerDown; break;
            case "pointerup": property = Zui.PointerUp; break;
            default: return;
        }

        // Bubble
        foreach (var view in views)
        {
            var e = view.GetProperty(property);
            if (e != null)
                e(null, ev);
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

        _appWindow.CallPreRenderFrame();


        // Resize if the window size changed
        var view = _appWindow.View;
        if (_mainWindowSize != _canvas.DeviceSize 
            || _devicePixelRatio != _window.DevicePixelRatio
            || _appWindow.View.IsMeasureInvalid)
        {
            view.SetProperty(Zui.Magnification, _window.DevicePixelRatio);
            _mainWindowSize = _canvas.DeviceSize / _window.DevicePixelRatio;
            _devicePixelRatio = _window.DevicePixelRatio;
            var measureConext = new Layout.MeasureContext(_canvas.Context);
            view.Measure(_mainWindowSize, measureConext);
            view.Arrange(new Rect(new(0, 0), _mainWindowSize), measureConext);
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
        _renderContext.SetCurrentViewInternal(view);
        _renderContext.PushDeviceClip(new Rect(new(), view.toDevice(_mainWindowSize)));
        RenderView(view);
        _renderContext.PopDeviceClip(view);
        Debug.Assert(_renderContext.ClipLevel == 0);
        _renderContext.SetCurrentViewInternal(null);

        _totalMs += timer.ElapsedMilliseconds;
    }


    void RenderView(View view)
    {
        // Quick exit for invisible
        if (!view.GetStyle(Zui.IsVisible).Or(true))
            return;


        // Render panel background and border (without clipping since we know it's always in bounds)
        _renderContext.SetCurrentViewInternal(view);
        DrawHelper.DrawBackground(view, _renderContext);

        // Clip the content rect if requested
        if (view.GetProperty(Zui.Clip).Or(false))
            _renderContext.PushDeviceClip(view.toDevice(view.ContentRect));

        try
        {
            var draw = view.Draw;
            if (draw is not null)
                draw.Draw(view, _renderContext);

            foreach (var child in view.Children)
                RenderView(child);

            if (draw is not null)
            {
                _renderContext.SetCurrentViewInternal(view);
                draw.DrawOver(view, _renderContext);
            }
        }
        finally
        {
            _renderContext.PopDeviceClip(view);
        }

    }

}
