
using System.Diagnostics;
using ZurfurGui.Base;
using ZurfurGui.Controls;
using ZurfurGui.Platform;
using ZurfurGui.Property;
using ZurfurGui.Styles;
using ZurfurGui.Windows;
using static ZurfurGui.Render.RenderContext;

namespace ZurfurGui.Render;

public class Renderer
{
    OsWindow _window;
    OsCanvas _canvas;
    AppWindow _appWindow;
    RenderContext _renderContext;
    PointerOver _pointerHover;
    OsDrawBuffer _drawBuffer;
    ObjectCache<string> _stringCache;

    public struct RendererStats
    {
        public long FrameCount;
        public long MeasureFrameCount;
        public long StyleFrameCount;
        public double TotalMs;
        public double DrawMs;
        public int DrawBufferLength;
        public double RenderMs => TotalMs - DrawMs;

        public static RendererStats operator -(RendererStats a, RendererStats b)
        {
            return new RendererStats
            {
                FrameCount = a.FrameCount - b.FrameCount,
                MeasureFrameCount = a.MeasureFrameCount - b.MeasureFrameCount,
                StyleFrameCount = a.StyleFrameCount - b.StyleFrameCount,
                TotalMs = a.TotalMs - b.TotalMs,
                DrawMs = a.DrawMs - b.DrawMs,
            };

        }
    }

    RendererStats _stats;

    int _second;

    double _devicePixelRatio = 0;
    Size _mainWindowSize = new();


    public bool FpsUpdatedOnceASecond { get; private set; }
    public OsCanvas Canvas => _canvas;
    public OsWindow Window => _window;

    public RenderContext.RenderContextStats RenderContextStats => _renderContext.RenderStats;
    
    public RendererStats Stats => _stats;


    public Renderer(OsWindow window, OsCanvas canvas, AppWindow appWindow)
    {
        _window = window;
        _canvas = canvas;
        _appWindow = appWindow;
        _stringCache = new ObjectCache<string>(_canvas.Context.MarshalString);
        _drawBuffer = new OsDrawBuffer(_stringCache);

        _renderContext = new RenderContext(_canvas.Context, _drawBuffer);
        _pointerHover = new PointerOver(_appWindow);
        _appWindow.SetAppWindowGlobals(this, _pointerHover);


        if (_canvas.PointerInput != null)
            throw new ArgumentException("Pointer input already taken", nameof(_canvas));
        _canvas.PointerInput = (ev) => _pointerHover.PointerInput(ev);
    }

    public void RenderFrame()
    {
        var timer = Stopwatch.StartNew();
        _drawBuffer.Reset();
        var stringTotal = _stringCache.TotalAccesses;

        var appView = _appWindow.View;
        ResizeAppWindow(appView);

        _appWindow.CallPreRenderFrame();

        InvalidateStyleFrame(appView);
        MeasureFrame(appView);
        RenderFrame(appView);

        var drawTimeStart = timer.Elapsed.TotalMilliseconds;
        _canvas.Context.DrawBuffer(_drawBuffer);
        var totalTimer = timer.Elapsed.TotalMilliseconds;

        // Stats
        _stats.FrameCount++;
        _stats.TotalMs += totalTimer;
        _stats.DrawMs += totalTimer - drawTimeStart;
        _stats.DrawBufferLength = _drawBuffer.Length;
        var now = DateTime.UtcNow;
        FpsUpdatedOnceASecond = now.Second != _second;
        if (FpsUpdatedOnceASecond)
            _second = now.Second;

        // Purge the string cache if it has grown too large.
        // Needs to be big enough to hold all strings in the frame
        var frameStringCount = _stringCache.TotalAccesses - stringTotal;
        _stringCache.PurgeLru(Math.Max(1000, (int)frameStringCount * 2));
    }


    // Resize the app window, only if it has changed
    private void ResizeAppWindow(View appView)
    {
        var devicePixelRatio = _window.DevicePixelRatio;
        var deviceSize = _canvas.DeviceSize;
        if (_mainWindowSize != deviceSize / devicePixelRatio
            || _devicePixelRatio != devicePixelRatio)
        {
            _mainWindowSize = deviceSize / devicePixelRatio;
            _devicePixelRatio = devicePixelRatio;
            appView.SetProperty(Panel.Magnification, devicePixelRatio);
            appView.InvalidateMeasure();
        }
    }

    private void InvalidateStyleFrame(View appView)
    {
        if (((appView.Flags | appView.FlagsChild) & (ViewFlags.StyleThis | ViewFlags.StyleDown)) != ViewFlags.None)
        {
            _stats.StyleFrameCount++;
            InvalidateStyle(appView, false);
        }
    }

    void InvalidateStyle(View view, bool nukem)
    {
        if (view.Flags.HasFlag(ViewFlags.StyleDown))
            nukem = true;
        var needsClearCache = nukem || view.Flags.HasFlag(ViewFlags.StyleThis);
        var needsChildTraverse = nukem || (view.FlagsChild & (ViewFlags.StyleThis | ViewFlags.StyleDown)) != ViewFlags.None;
        view.Flags &= ~ViewFlags.StyleThis;
        view.FlagsChild &= ~ViewFlags.StyleThis;

        if (needsClearCache)
        {
            view.InvalidateStyleCacheInternal();
            view.InvalidateMeasure();
            view.InvalidateDraw();
        }

        if (needsChildTraverse)
            foreach (var child in view.Children)
                InvalidateStyle(child, nukem);
    }

    private void MeasureFrame(View appView)
    {
        // Re-measure if necessary
        if ((appView.Flags | appView.FlagsChild).HasFlag(ViewFlags.Measure))
        {
            _stats.MeasureFrameCount++;
            var measureConext = new Layout.MeasureContext(_canvas.Context);
            appView.Measure(_mainWindowSize, measureConext);
            appView.Arrange(new Rect(new(0, 0), _mainWindowSize), measureConext);
        }
    }

    private void RenderFrame(View appView)
    {
        // Re-draw if any flags changed
        if ((appView.Flags | appView.FlagsChild) != ViewFlags.None)
        {
            try
            {
                _renderContext.SetPointerPosition(_pointerHover.PointerDevicePosition);
                RenderView(appView, true);
            }
            finally
            {
                _renderContext.ClearClips();
            }
        }
    }

    void RenderView(View view, bool forceClip)
    {
        // Quick exit for invisible
        if (!view._measureCache.IsVisible)
            return;

        view.Flags = ViewFlags.None;
        view.FlagsChild = ViewFlags.None;

        try
        {
            _renderContext.SetCurrentViewInternal(view);

            // Clip the content rect if requested
            var drawBufferIndex = _drawBuffer.Length;
            if (forceClip || view._measureCache.Clip)
                _renderContext.PushClip(view.ContentRect);

            DrawBackground(view);

            if (drawBufferIndex != _drawBuffer.Length)
                OsDrawBuffer.TransformBuffer(_drawBuffer.AsSpan(drawBufferIndex), view.Origin, view.Scale);

            foreach (var child in view.Children)
                RenderView(child, false);

            DrawOver(view);
        }
        finally
        {
            while (view._pushedClips != 0)
                _renderContext.PopClip(view);
        }
    }


    /// <summary>
    /// Draw the background, then the control's own drawing under all the content
    /// </summary>
    private void DrawBackground(View view)
    {
        // Quick exit when drawing outside the clip region
        var draw = view.Draw;
        if (_renderContext.DeviceClip.Intersect(new Rect(view.Origin, view.toDevice(view.Size))).Width == 0)
            if (draw == null || draw.PromiseToDrawInsideControl)
                return;

        DrawHelper.DrawBackground(view, _renderContext);
        if (draw is not null)
        {
            var clipIndex = view._pushedClips;
            draw.Draw(view, _renderContext);
            while (view._pushedClips > clipIndex)
                _renderContext.PopClip(view);
        }
    }

    /// <summary>
    /// Draw over all content controls
    /// </summary>
    private void DrawOver(View view)
    {
        var draw = view.Draw;
        if (draw is null)
            return;

        // Quick exit when drawing outside the clip region
        if (_renderContext.DeviceClip.Intersect(new Rect(view.Origin, view.toDevice(view.Size))).Width == 0)
            if (draw.PromiseToDrawInsideControl)
                return;

        _renderContext.SetCurrentViewInternal(view);
        var drawBufferIndex = _drawBuffer.Length;
        var clipIndex = view._pushedClips;
        draw.DrawOver(view, _renderContext);
        while (view._pushedClips > clipIndex)
            _renderContext.PopClip(view);

        OsDrawBuffer.TransformBuffer(_drawBuffer.AsSpan(drawBufferIndex), view.Origin, view.Scale);
    }


}
