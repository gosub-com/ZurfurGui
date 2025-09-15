
using System.Diagnostics;
using System.Runtime.InteropServices;
using ZurfurGui.Base;
using ZurfurGui.Controls;
using ZurfurGui.Platform;

namespace ZurfurGui.Draw;


public class Renderer
{
    OsWindow _window;
    OsCanvas _canvas;
    AppWindow _appWindow;
    DrawContext _renderContext;

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


    public Renderer(OsWindow window, OsCanvas canvas, AppWindow appWindow)
    {
        _window = window;
        _canvas = canvas;
        _renderContext = new DrawContext(_canvas.Context);

        _appWindow = appWindow;
        _appWindow.SetRenderWindow(window, canvas);

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

        if (!view.GetStyle(Zui.IsVisible, true))
            return null;

        // Check children first
        var views = view.Children;
        for (var i = views.Count - 1; i >= 0; i--)
        {
            var hit = FindHitTarget(views[i], target);
            if (hit != null)
                return hit;
        }

        if (!view.GetProperty(Zui.DisableHitTest, false))
        {
            // User content hit test
            if (view.Draw is Drawable renderable)
                if (renderable.IsHit(view, target))
                    return view;

            // Panel hit test
            if (DrawManager.IsHitPanel(view, target))
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

        var view = _appWindow.View;
        if (_mainWindowSize != _canvas.DeviceSize || _devicePixelRatio != _window.DevicePixelRatio
            || _appWindow.View.IsMeasureInvalid)
        {
            view.SetProperty(Zui.Magnification, _window.DevicePixelRatio);
            _mainWindowSize = _canvas.DeviceSize / _window.DevicePixelRatio;
            _devicePixelRatio = _window.DevicePixelRatio;
            var measureConext = new Layout.MeasureContext(_canvas.Context);
            view.Measure(_mainWindowSize, measureConext);
            view.Arrange(new Rect(new(0,0), _mainWindowSize), measureConext);
            view.PostArrange(new(), 1);
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
        _appWindow.SetRenderStats(_frameCount, _fps, (int)_avgMs);

        RenderView(view, new Rect(new(), view.toDevice(_mainWindowSize)));
        Debug.Assert(_renderContext.ClipLevel == 0);

        _totalMs += timer.ElapsedMilliseconds;
    }


    void RenderView(View view, Rect clip)
    {
        // Quick exit for invisible
        if (!view.GetStyle(Zui.IsVisible, true))
            return;

        var doClip = view.GetProperty(Zui.Clip, false);
        if (doClip)
        {
            // Skip drawing when fully clipped
            clip = clip.Intersect(view.toDevice(new Rect(new(), view.Size)));
            if (clip.Width <= 0 || clip.Height <= 0)
                return;
         
            _renderContext.ClipDevice(clip.X, clip.Y, clip.Width, clip.Height);
        }

        try
        {
            _renderContext.SetOrigin(view.Origin, view.Scale);

            // Render panel background & border
            DrawManager.Draw(view, _renderContext);

            var padding = view.GetStyle(Zui.Padding, null).Or(0) 
                + new Thickness(view.GetStyle(Zui.BorderWidth, null).Or(0));
            var contentRect = new Rect(new(), view.Size).Deflate(padding);

            var draw = view.Draw;
            if (draw is not null)
                draw.Draw(view, _renderContext, contentRect);
            
            foreach (var child in view.Children)
                RenderView(child, clip);

            if (draw is not null)
                draw.DrawOver(view, _renderContext, contentRect);
        }
        finally
        {
            if (doClip)
                _renderContext.UnClip();
        }

    }

}
