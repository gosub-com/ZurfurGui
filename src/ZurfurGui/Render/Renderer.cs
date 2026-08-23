
using System.Diagnostics;
using ZurfurGui.Base;
using ZurfurGui.Collections;
using ZurfurGui.Controls;
using ZurfurGui.Input;
using ZurfurGui.Platform;
using ZurfurGui.Windows;

namespace ZurfurGui.Render;

public class Renderer
{
    private static readonly bool DRAW_DIRTY_RECT = false;
    private static readonly bool DIRTY_RECT_ENABLE = true;


    /// <summary>
    /// Updated by View.ExpandRemovedChildDirtyRect to track removed view dirty rectangles
    /// </summary>
    internal static Rect s_removedChildDirtyRectDevice;


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
        InvalidateFlagsDown(appView, ViewFlags.None);
        appView.Measure(_mainWindowSize, _measureContext);
        appView.Arrange(new Rect(new(0, 0), _mainWindowSize), _measureContext);

        // Render
        var renderStartTime = timer.Elapsed.TotalMilliseconds;
        var dirtyRect = s_removedChildDirtyRectDevice;
        s_removedChildDirtyRectDevice = Rect.Empty;
        RenderView(appView, appView.toDevice(appView.ContentRect), ref dirtyRect);

        // Composite
        var compositeStartTime = timer.Elapsed.TotalMilliseconds;
        CompositeMain(appView, appView.toDevice(appView.ContentRect), dirtyRect);

        // Draw invalid rect outline
        if (DRAW_DIRTY_RECT && !dirtyRect.IsEmpty)
        {
            var dirtyRectOutline = new OsRenderBuffer();
            dirtyRectOutline.SetStrokeColorWidth(Colors.Red, 3);
            dirtyRectOutline.StrokeRect(dirtyRect.X, dirtyRect.Y, dirtyRect.Width, dirtyRect.Height, 5);
            _presentBuffer.Composite(dirtyRectOutline, _stringCache, new Point(0, 0), 1);
        }

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

    void InvalidateFlagsDown(View view, ViewFlags nukeFlags)
    {
        nukeFlags = (nukeFlags | view.Flags) & (ViewFlags.StyleDown | ViewFlags.DirtyDown);

        // Invalidate style cache
        if (nukeFlags.HasFlag(ViewFlags.StyleDown) || view.Flags.HasFlag(ViewFlags.Style))
            view.InvalidateStyleCacheInternal();

        if (nukeFlags.HasFlag(ViewFlags.DirtyDown))
            view.SetFlags(ViewFlags.Dirty);


        if (!view.IsVisible)
            return;


        var needsChildTraverse = nukeFlags != ViewFlags.None 
            || (view.FlagsChild & (ViewFlags.Style | ViewFlags.StyleDown)) != ViewFlags.None
            || (view.FlagsChild & (ViewFlags.Dirty | ViewFlags.DirtyDown)) != ViewFlags.None;

        if (needsChildTraverse)
            foreach (var child in view.Children)
                InvalidateFlagsDown(child, nukeFlags);
    }

    /// <summary>
    /// Render the view tree into the internal view's cache buffers (_renderOver, etc.).
    /// Update the dirty rectangles.
    /// </summary>
    void RenderView(View view, Rect deviceClip, ref Rect dirtyRect)
    {
        // Quick exit for invisible
        if (!view.IsVisible)
        {
            if (view._measureCache.VisibleAtLastRender)
                UpdateInvisibleDirtyRect(view, ref dirtyRect);
            view._measureCache.VisibleAtLastRender = false;
            return;
        }
        view._measureCache.VisibleAtLastRender = true;

        if (view.GetStyle(Panel.Clip))
            deviceClip = deviceClip.Intersect(view.toDevice(view.ContentRect));

        var needsRender = view.Flags.HasFlag(ViewFlags.Render);
        var dirtyVisualRect = view.Flags.HasFlag(ViewFlags.Dirty);
        var renderer = view.Render;
        var newVisualRect = Rect.Empty;

        if (needsRender)
        {
            // Render background
            _stats.RenderCount++;
            _renderContext.ClearRenderBuffer();
            RenderHelper.RenderBackground(view, _renderContext);
                       
            // Call user render function
            if (renderer is not null)
            {
                renderer.Render(view, _renderContext);
                _renderContext.FlushClips();
            }
            view._renderUnderBuffer = _renderContext.CloneRenderBuffer();
            newVisualRect = newVisualRect.Union(view._renderUnderBuffer.MeasureBounds(_measureContext));
        }

        // Recurse down the tree if any child needs render or dirty visual rect
        if (view.FlagsChild.HasFlag(ViewFlags.Render)
            || view.FlagsChild.HasFlag(ViewFlags.Dirty))
        {
            foreach (var child in view.Children)
                RenderView(child, deviceClip, ref dirtyRect);
        }

        if (needsRender)
        {
            // Render foreground
            _renderContext.ClearRenderBuffer();
            if (renderer is not null)
            {
                renderer.RenderOver(view, _renderContext);
                _renderContext.FlushClips();
                view._renderOverBuffer = _renderContext.CloneRenderBuffer();
                newVisualRect = newVisualRect.Union(view._renderOverBuffer.MeasureBounds(_measureContext));
            }
        }

        if (needsRender || dirtyVisualRect)
        {
            // Dirty the old device rect
            dirtyRect = dirtyRect.Union(view._measureCache.VisualDeviceBoundsAtLastRender.Intersect(deviceClip));

            // Update visual bounds
            if (needsRender)
                view.VisualBounds = newVisualRect;

            // Dirty the new device rect
            view._measureCache.VisualDeviceBoundsAtLastRender = view.toDevice(view.VisualBounds);
            dirtyRect = dirtyRect.Union(view._measureCache.VisualDeviceBoundsAtLastRender.Intersect(deviceClip));
        }
    }

    void UpdateInvisibleDirtyRect(View view, ref Rect dirtyRect)
    {
        if (view.IsVisible || view._measureCache.VisibleAtLastRender)
        {
            dirtyRect = dirtyRect.Union(view._measureCache.VisualDeviceBoundsAtLastRender);
            foreach (var child in view.Children)
                UpdateInvisibleDirtyRect(child, ref dirtyRect);
        }

    }


    void CompositeMain(View view, Rect deviceClip, Rect dirtyRect)
    {
        if (dirtyRect.IsEmpty)
            return;

        _presentBuffer.Clear();

        if (DIRTY_RECT_ENABLE)
        {
            dirtyRect = dirtyRect.Inflate(1);
            dirtyRect = new Rect(Math.Floor(dirtyRect.X), Math.Floor(dirtyRect.Y),
                Math.Ceiling(dirtyRect.Width), Math.Ceiling(dirtyRect.Height));
            _presentBuffer.Clip(dirtyRect);
        }

        try
        {
            Composite(view, deviceClip, dirtyRect);
        }
        finally
        {
            if (DIRTY_RECT_ENABLE)
                _presentBuffer.PopClip();
        }
    }

    /// <summary>
    /// Composite the frame into _presentBuffer.
    /// </summary>
    void Composite(View view, Rect deviceClip, Rect dirtyRect)
    {
        // Quick exit for invisible
        if (!view.IsVisible)
            return;

        bool doClip = false;
        try
        {
            view.Flags = ViewFlags.None;
            view.FlagsChild = ViewFlags.None;

            // Clip the content rect if requested
            var presentBufferIndex = _presentBuffer.CommandsLength;
            if (view.GetStyle(Panel.Clip))
            {
                doClip = true;
                deviceClip = deviceClip.Intersect(view.toDevice(view.ContentRect));
                _presentBuffer.Clip(deviceClip);
            }

            // Do not composite if outside clipping region
            bool isFullyClipped = false;
            var drawDeviceRect = deviceClip.Intersect(view.toDevice(view.VisualBounds));
            if (drawDeviceRect.IsEmpty
                || DIRTY_RECT_ENABLE && drawDeviceRect.Intersect(dirtyRect).IsEmpty)
                isFullyClipped = true;

            var presented = false;
            if (!isFullyClipped && view._renderUnderBuffer is OsRenderBuffer bufferUnder and { CommandsLength: > 0 })
            {
                presented = true;
                _presentBuffer.Composite(bufferUnder, _stringCache, view.Origin, view.Scale);
            }

            foreach (var child in view.Children)
                Composite(child, deviceClip, dirtyRect);

            if (!isFullyClipped && view._renderOverBuffer is OsRenderBuffer bufferOver and { CommandsLength: > 0 })
            {
                presented = true;
                _presentBuffer.Composite(bufferOver, _stringCache, view.Origin, view.Scale);
            }

            if (presented)
                _stats.CompositeCount++;
        }
        finally
        {
            if (doClip)
                _presentBuffer.PopClip();
        }
    }


}
