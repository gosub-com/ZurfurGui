
using System.Diagnostics;
using ZurfurGui.Base;
using ZurfurGui.Collections;
using ZurfurGui.Controls;
using ZurfurGui.Input;
using ZurfurGui.Platform;
using ZurfurGui.Property;
using ZurfurGui.Windows;

namespace ZurfurGui.Render;

public class Renderer
{
    OsWindow _window;
    OsCanvas _canvas;
    AppWindow _appWindow;
    MeasureContext _measureContext;
    RenderContext _renderContext;
    PointerOver _pointerHover;
    OsRenderBuffer _presentBuffer;
    ObjectCache<string> _stringCache;

    public struct RendererStats
    {
        public long FrameCount;
        public long MeasureCount;
        public long RenderCount;
        public long CompositeCount;
        public long StyleFrameCount;

        public double TotalMs;
        public double MeasureMs;
        public double RenderMs;
        public double CompositeMs;
        public double PresentMs;
        public int PresentBufferLength;

        public static RendererStats operator -(RendererStats a, RendererStats b)
        {
            return new RendererStats
            {
                FrameCount = a.FrameCount - b.FrameCount,
                MeasureCount = a.MeasureCount - b.MeasureCount,
                RenderCount = a.RenderCount - b.RenderCount,
                CompositeCount = a.CompositeCount - b.CompositeCount,
                StyleFrameCount = a.StyleFrameCount - b.StyleFrameCount,
                TotalMs = a.TotalMs - b.TotalMs,
                MeasureMs = a.MeasureMs - b.MeasureMs,
                RenderMs = a.RenderMs - b.RenderMs,
                CompositeMs = a.CompositeMs - b.CompositeMs,
                PresentMs = a.PresentMs - b.PresentMs,
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

    internal PointerOver PointerHover => _pointerHover;


    public Renderer(OsWindow window, OsCanvas canvas, AppWindow appWindow)
    {
        _window = window;
        _canvas = canvas;
        _appWindow = appWindow;
        _stringCache = new ObjectCache<string>(_canvas.Context.MarshalString);
        _presentBuffer = new OsRenderBuffer();

        _measureContext = new MeasureContext(_canvas.Context);
        _renderContext = new RenderContext(_measureContext);
        _pointerHover = new PointerOver(_appWindow);
        _appWindow.SetAppWindowGlobals(this);


        if (_canvas.PointerInput != null)
            throw new ArgumentException("Pointer input already taken", nameof(_canvas));
        _canvas.PointerInput = (ev) => _pointerHover.PointerInput(ev);
    }

    public void RenderFrame()
    {
        // Setup and resize canvas if necessary
        var timer = Stopwatch.StartNew();
        var stringTotal = _stringCache.TotalAccesses;
        var appView = _appWindow.View;
        appView.SetProperty(Panel.Clip, true);
        ResizeAppWindow(appView);
        _appWindow.CallPreRenderFrame();

        // Measure
        InvalidateStyles(appView);
        appView.Measure(_mainWindowSize, _measureContext);
        appView.Arrange(new Rect(new(0, 0), _mainWindowSize), _measureContext);
        ClearFlag(appView, ViewFlags.Measure);

        // Render
        var renderStartTime = timer.Elapsed.TotalMilliseconds;
        RenderView(appView);

        // Composite
        var compositeStartTime = timer.Elapsed.TotalMilliseconds;
        _presentBuffer.Clear();
        Composite(appView, appView.toDevice(appView.ContentRect));

        // Present
        var presentTimeStart = timer.Elapsed.TotalMilliseconds;
        _canvas.Context.Present(_presentBuffer);
        var totalTimer = timer.Elapsed.TotalMilliseconds;

        // Stats
        _stats.FrameCount++;
        _stats.TotalMs += totalTimer;
        _stats.MeasureMs += renderStartTime;
        _stats.RenderMs += compositeStartTime - renderStartTime;
        _stats.CompositeMs += presentTimeStart - compositeStartTime;
        _stats.PresentMs += totalTimer - presentTimeStart;
        _stats.PresentBufferLength = _presentBuffer.CommandsLength;
        _stats.MeasureCount = View.s_measureCount;
        var now = DateTime.UtcNow;
        FpsUpdatedOnceASecond = now.Second != _second;
        if (FpsUpdatedOnceASecond)
            _second = now.Second;

        // Purge the string cache if it has grown too large.
        // Needs to be big enough to hold all strings in the frame
        var frameStringCount = _stringCache.TotalAccesses - stringTotal;
        _stringCache.PurgeLru((int)frameStringCount + 1000);
        _measureContext.FrameDone();
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

    private void InvalidateStyles(View appView)
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
        }

        if (needsChildTraverse)
            foreach (var child in view.Children)
                InvalidateStyle(child, nukem);
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


    /// <summary>
    /// Render the view tree into the internal view's cache buffers (_renderOver, etc.)
    /// </summary>
    void RenderView(View view)
    {
        // Quick exit for invisible
        if (!view._measureCache.IsVisible)
            return;

        var needsRender = view.Flags.HasFlag(ViewFlags.Render);
        var childNeedsRender = view.FlagsChild.HasFlag(ViewFlags.Render);

        view.Flags = ViewFlags.None;
        view.FlagsChild = ViewFlags.None;

        var renderer = view.Render;
        _renderContext.ClearRenderBuffer();

        if (needsRender)
        {
            // Render background
            _stats.RenderCount++;
            RenderHelper.RenderBackground(view, _renderContext);
            if (renderer is not null)
                renderer.Render(view, _renderContext);
            _renderContext.FlushClips();
            view._renderUnderBuffer = _renderContext.CloneRenderBuffer();
        }

        if (childNeedsRender)
        {
            foreach (var child in view.Children)
                RenderView(child);
        }

        if (needsRender)
        {
            // Render foreground
            _renderContext.ClearRenderBuffer();
            if (renderer is not null)
                renderer.RenderOver(view, _renderContext);
            _renderContext.FlushClips();
            view._renderOverBuffer = _renderContext.CloneRenderBuffer();
        }
    }


    /// <summary>
    /// Composite the frame into _presentBuffer.
    /// </summary>
    void Composite(View view,Rect deviceClip)
    {
        bool clipped = false;
        try
        {
            // Clip the content rect if requested
            var presentBufferIndex = _presentBuffer.CommandsLength;
            if (view.GetStyle(Panel.Clip))
            {
                clipped = true;
                deviceClip = deviceClip.Intersect(view.toDevice(view.ContentRect));
                _presentBuffer.Clip(deviceClip);
            }

            // TBD: Do not render if outside clipping region
            bool present = true;
            if (deviceClip.Intersect(new Rect(view.Origin, view.toDevice(view.Size))).Width == 0)
                present = false;

            var presented = false;
            if (present && view._renderUnderBuffer is OsRenderBuffer bufferUnder and { CommandsLength: > 0 })
            {
                presented = true;
                _presentBuffer.Composite(bufferUnder, _stringCache, view.Origin, view.Scale);
            }

            foreach (var child in view.Children)
                Composite(child, deviceClip);

            if (present && view._renderOverBuffer is OsRenderBuffer bufferOver and { CommandsLength: > 0 })
            {
                presented = true;
                _presentBuffer.Composite(bufferOver, _stringCache, view.Origin, view.Scale);
            }

            if (presented)
                _stats.CompositeCount++;
        }
        finally
        {
            if (clipped)
                _presentBuffer.PopClip();
        }
    }


}
