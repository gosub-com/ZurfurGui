
using System.Diagnostics;
using ZurfurGui.Base;
using ZurfurGui.Platform;
using ZurfurGui.Property;
using ZurfurGui.Controls;

namespace ZurfurGui.Render;

public class Renderer
{
    OsWindow _window;
    OsCanvas _canvas;
    AppWindow _appWindow;
    RenderContext _renderContext;
    PointerOver _pointerHover;

    long _frameLastCountSecond;
    int _second;
    long _totalMs;

    double _devicePixelRatio = 0;
    Size _mainWindowSize = new();


    public long Fps { get; private set; }
    public bool FpsUpdatedOnceASecond { get; private set; }
    public double AvgMs { get; private set; }
    public long FrameCount { get; private set; }
    public long DrawCount { get; private set; }
    public long MeasureCount { get; private set; }
    public long StyleCount { get; private set; }
    public OsCanvas Canvas => _canvas;
    public OsWindow Window => _window;
    public RenderContext RenderContext => _renderContext;


    public Renderer(OsWindow window, OsCanvas canvas, AppWindow appWindow)
    {
        _window = window;
        _canvas = canvas;
        _appWindow = appWindow;

        _renderContext = new RenderContext(_canvas.Context);
        _pointerHover = new PointerOver(_appWindow);
        _appWindow.SetAppWindowGlobals(this, _pointerHover);

        if (_canvas.PointerInput != null)
            throw new ArgumentException("Pointer input already taken", nameof(_canvas));
        _canvas.PointerInput = (ev) => _pointerHover.PointerInput(ev);
    }

    public void RenderFrame()
    {
        var timer = Stopwatch.StartNew();

        // Resize if the window size changed
        var devicePixelRatio = _window.DevicePixelRatio;
        var deviceSize = _canvas.DeviceSize;
        var appView = _appWindow.View;
        if (_mainWindowSize != deviceSize / devicePixelRatio
            || _devicePixelRatio != devicePixelRatio)
        {
            _mainWindowSize = deviceSize / devicePixelRatio;
            _devicePixelRatio = devicePixelRatio;
            appView.SetProperty(Zui.Magnification, devicePixelRatio);
            appView.InvalidateMeasure();
        }

        _appWindow.CallPreRenderFrame();


        if (((appView.Flags | appView.FlagsChild) & (ViewFlags.ReStyleThis | ViewFlags.ReStyleDown)) != ViewFlags.None)
        {
            StyleCount++;
            InvalidateStyle(appView, false);
        }

        // Re-measure if necessary
        if ((appView.Flags | appView.FlagsChild).HasFlag(ViewFlags.ReMeasure))
        {
            MeasureCount++;
            var measureConext = new Layout.MeasureContext(_canvas.Context);
            appView.Measure(_mainWindowSize, measureConext);
            appView.Arrange(new Rect(new(0, 0), _mainWindowSize), measureConext);
        }

        // Re-draw if any flags changed
        if ((appView.Flags | appView.FlagsChild) != ViewFlags.None)
        {
            _renderContext.SetPointerPosition(_pointerHover.PointerPosition);
            _renderContext.SetCurrentViewInternal(appView);
            _renderContext.PushDeviceClip(new Rect(new(), appView.toDevice(_mainWindowSize)));
            DrawCount++;
            RenderView(appView);
            _renderContext.PopDeviceClip(appView);
            Debug.Assert(_renderContext.ClipLevel == 0);
            _renderContext.SetCurrentViewInternal(null);
        }

        _totalMs += timer.ElapsedMilliseconds;

        FrameCount++;
        var now = DateTime.UtcNow;
        FpsUpdatedOnceASecond = now.Second != _second;
        if (FpsUpdatedOnceASecond)
        {
            _second = now.Second;
            Fps = FrameCount - _frameLastCountSecond;
            _frameLastCountSecond = FrameCount;
            if (Fps != 0)
                AvgMs = _totalMs / Fps;
            _totalMs = 0;
        }
    }

    void InvalidateStyle(View view, bool nukem)
    {
        if (view.Flags.HasFlag(ViewFlags.ReStyleDown))
            nukem = true;
        var needsClearCache = nukem || view.Flags.HasFlag(ViewFlags.ReStyleThis);
        var needsChildTraverse = nukem || (view.FlagsChild & (ViewFlags.ReStyleThis | ViewFlags.ReStyleDown)) != ViewFlags.None;
        view.Flags &= ~ViewFlags.ReStyleThis;
        view.FlagsChild &= ~ViewFlags.ReStyleThis;

        if (needsClearCache)
        {
            view.ClearStyleCache();
            view.InvalidateMeasure();
            view.InvalidateDraw();
        }

        if (needsChildTraverse)
            foreach (var child in view.Children)
                InvalidateStyle(child, nukem);
    }


    void RenderView(View view)
    {
        // Quick exit for invisible
        if (!view._cache.IsVisible)
            return;

        view.Flags = ViewFlags.None;
        view.FlagsChild = ViewFlags.None;

        // Render panel background and border (without clipping since we know it's always in bounds)
        _renderContext.SetCurrentViewInternal(view);
        DrawHelper.DrawBackground(view, _renderContext);

        // Clip the content rect if requested
        if (view._cache.Clip)
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
