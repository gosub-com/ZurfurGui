
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
    PointerOver _pointerHover;

    long _frameLastCountSecond;
    int _second;
    long _totalMs;

    double _devicePixelRatio = double.NaN;
    Size _mainWindowSize = new(double.NaN, double.NaN);


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

        _appWindow.CallPreRenderFrame();


        // Resize if the window size changed
        var appView = _appWindow.View;
        if (_mainWindowSize != _canvas.DeviceSize 
            || _devicePixelRatio != _window.DevicePixelRatio)
        {
            appView.SetProperty(Zui.Magnification, _window.DevicePixelRatio);
            _mainWindowSize = _canvas.DeviceSize / _window.DevicePixelRatio;
            _devicePixelRatio = _window.DevicePixelRatio;
            appView.InvalidateMeasure();
        }

        if ((appView.Flags | appView.FlagsChild).HasFlag(ViewFlags.RePseudo))
        {
            StyleCount++;
            InvalidatePseudo(appView);
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

    void InvalidatePseudo(View view)
    {
        var needsClearCache = view.Flags.HasFlag(ViewFlags.RePseudo);
        var needsChildTraverse = view.FlagsChild.HasFlag(ViewFlags.RePseudo);
        view.Flags &= ~ViewFlags.RePseudo;
        view.FlagsChild &= ~ViewFlags.RePseudo;

        if (needsClearCache)
        {
            view.ClearStyleCache();
            view.InvalidateMeasure();
            view.InvalidateDraw();
        }

        if (needsChildTraverse)
            foreach (var child in view.Children)
                InvalidatePseudo(child);
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
