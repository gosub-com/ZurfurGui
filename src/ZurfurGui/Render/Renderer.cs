
using System.Diagnostics;
using ZurfurGui.Base;
using ZurfurGui.Collections;
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
        public long InvalidMeasureCount;
        public long InvalidDrawCount;
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
                InvalidMeasureCount = a.InvalidMeasureCount - b.InvalidMeasureCount,
                InvalidDrawCount = a.InvalidDrawCount - b.InvalidDrawCount,
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
        _drawBuffer = new OsDrawBuffer();

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
        _drawBuffer.Clear();
        var stringTotal = _stringCache.TotalAccesses;

        var appView = _appWindow.View;
        appView.SetProperty(Panel.Clip, true);

        ResizeAppWindow(appView);

        _appWindow.CallPreRenderFrame();

        InvalidateStyleFrame(appView);
        MeasureFrame(appView);
        RenderFrame(appView);

        _drawBuffer.Clear();
        DrawFrame(appView, _drawBuffer, appView.toDevice(appView.ContentRect));

        var drawTimeStart = timer.Elapsed.TotalMilliseconds;
        _canvas.Context.DrawBuffer(_drawBuffer);
        var totalTimer = timer.Elapsed.TotalMilliseconds;

        // Stats
        _stats.FrameCount++;
        _stats.TotalMs += totalTimer;
        _stats.DrawMs += totalTimer - drawTimeStart;
        _stats.DrawBufferLength = _drawBuffer.CommandsLength;
        _stats.InvalidMeasureCount = View.s_measureCount;
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
        
        view.Flags &= ~(ViewFlags.StyleThis | ViewFlags.StyleDown);
        view.FlagsChild &= ~(ViewFlags.StyleThis | ViewFlags.StyleDown);

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
            var measureConext = new Layout.MeasureContext(_canvas.Context);
            appView.Measure(_mainWindowSize, measureConext);
            appView.Arrange(new Rect(new(0, 0), _mainWindowSize), measureConext);
            ClearFlag(appView, ViewFlags.Measure);
        }
    }

    static void ClearFlag(View view, ViewFlags flags)
    {
        view.Flags &= ~flags;
        if (view.FlagsChild.HasFlag(flags))
        {
            view.FlagsChild &= ~flags;
            foreach (var child in view.Children)
                ClearFlag(child, flags);
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
                RenderView(appView);
            }
            finally
            {
                _renderContext.FlushClips();
            }
        }
    }

    void RenderView(View view)
    {
        // Quick exit for invisible
        if (!view._measureCache.IsVisible)
            return;

        var flags = view.Flags.HasFlag(ViewFlags.Draw);
        var flagsChild = view.FlagsChild.HasFlag(ViewFlags.Draw);

        view.Flags = ViewFlags.None;
        view.FlagsChild = ViewFlags.None;

        try
        {
            var draw = view.Draw;
            if (flags)
            {
                // Render background
                _stats.InvalidDrawCount++;
                _drawBuffer.Clear();
                DrawHelper.DrawBackground(view, _renderContext);
                if (draw is not null)
                    draw.Draw(view, _renderContext);
                _renderContext.FlushClips();
                view._drawUnderBuffer = _drawBuffer.Clone();
            }

            if (flagsChild)
            {
                foreach (var child in view.Children)
                    RenderView(child);
            }

            if (flags)
            {
                // Render foreground
                _drawBuffer.Clear();
                if (draw is not null)
                    draw.DrawOver(view, _renderContext);
                _renderContext.FlushClips();
                view._drawOverBuffer = _drawBuffer.Clone();
            }
        }
        finally
        {
            _renderContext.FlushClips();
        }
    }


    void DrawFrame(View view, OsDrawBuffer drawBuffer, Rect deviceClip)
    {
        bool clipped = false;
        try
        {
            // Clip the content rect if requested
            var drawBufferIndex = _drawBuffer.CommandsLength;
            if (view._measureCache.Clip)
            {
                clipped = true;
                deviceClip = deviceClip.Intersect(view.toDevice(view.ContentRect));
                drawBuffer.Clip(deviceClip);
            }

            // TBD: Do not draw if outside clipping region
            bool draw = true;
            if (deviceClip.Intersect(new Rect(view.Origin, view.toDevice(view.Size))).Width == 0)
                draw = false;

            if (draw)
                if (view._drawUnderBuffer is OsDrawBuffer bufferUnder and { CommandsLength: > 0 })
                    drawBuffer.DrawBuffer(bufferUnder, _stringCache, view.Origin, view.Scale);

            foreach (var child in view.Children)
                DrawFrame(child, drawBuffer, deviceClip);

            if (draw)
                if (view._drawOverBuffer is OsDrawBuffer bufferOver and { CommandsLength: > 0})
                    drawBuffer.DrawBuffer(bufferOver, _stringCache, view.Origin, view.Scale);
        }
        finally
        {
            if (clipped)
                drawBuffer.PopClip();
        }
    }


}
