
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
    PointerRouter _pointerRouter;
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

    internal PointerRouter PointerRouter => _pointerRouter;


    public Renderer(OsWindow window, OsCanvas canvas, AppWindow appWindow)
    {
        _window = window;
        _canvas = canvas;
        _appWindow = appWindow;
        _stringCache = new ObjectCache<string>(_canvas.Context.MarshalString);
        _presentBuffer = new OsRenderBuffer();

        _measureContext = new MeasureContext(_canvas.Context);
        _renderContext = new RenderContext(_measureContext);
        _pointerRouter = new PointerRouter(_appWindow);
        _appWindow.SetAppWindowGlobals(this);


        if (_canvas.PointerInput != null)
            throw new ArgumentException("Pointer input already taken", nameof(_canvas));
        _canvas.PointerInput = (ev) => _pointerRouter.PointerInput(ev);
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
        InvalidateClipChanges(appView, appView.toDevice(appView.ContentRect));
        RenderView(appView, appView.toDevice(appView.ContentRect), ref dirtyRect);

        // Composite
        var compositeStartTime = timer.Elapsed.TotalMilliseconds;
        CompositeAppWindow(dirtyRect);

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
        nukeFlags = (nukeFlags | view.Flags) & (ViewFlags.StyleChild | ViewFlags.MovedChild);

        // Invalidate style cache
        if (nukeFlags.HasFlag(ViewFlags.StyleChild) || view.Flags.HasFlag(ViewFlags.Style))
            view.InvalidateStyleCacheInternal();

        if (nukeFlags.HasFlag(ViewFlags.MovedChild))
            view.SetFlags(ViewFlags.Moved);


        if (!view.IsVisible)
            return;


        var needsChildTraverse = nukeFlags != ViewFlags.None 
            || (view.FlagsChild & (ViewFlags.Style | ViewFlags.StyleChild)) != ViewFlags.None
            || (view.FlagsChild & (ViewFlags.Moved | ViewFlags.MovedChild)) != ViewFlags.None;

        if (needsChildTraverse)
            foreach (var child in view.Children)
                InvalidateFlagsDown(child, nukeFlags);
    }

    void InvalidateClipChanges(View view, Rect deviceClip)
    {
        if (!view.IsVisible)
            return;

        if (view.GetProperty(Panel.Clip))
            deviceClip = deviceClip.Intersect(view.toDevice(view.ContentRect));

        var clientClip = view.toClient(deviceClip);
        var clipChanged = view._renderClientClip != clientClip;
        if (clipChanged)
        {
            if (view._renderResult == RenderResult.RenderedClip
                || view._renderResultOver == RenderResult.RenderedClip)
            {
                view.SetFlags(ViewFlags.ClientClipChanged);
            }

            view._renderClientClip = clientClip;
        }

        if (!clipChanged
            && !view.FlagsChild.HasFlag(ViewFlags.Render)
            && !view.FlagsChild.HasFlag(ViewFlags.Moved)
            && !view.FlagsChild.HasFlag(ViewFlags.ClientClipChanged))
        {
            return;
        }

        foreach (var child in view.Children)
            InvalidateClipChanges(child, deviceClip);
    }

    /// <summary>
    /// Render the view tree into the internal view's cache buffers (_renderOver, etc.).
    /// Calculate the dirtyRect while walking the tree. The dirtyRect is the union of the
    /// old and new visual bounds of all views that were rendered or had a dirty visual rect.
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

        if (view.GetProperty(Panel.Clip))
            deviceClip = deviceClip.Intersect(view.toDevice(view.ContentRect));

        var needsRender = view.Flags.HasFlag(ViewFlags.Render);
        var clientClipChanged = view.Flags.HasFlag(ViewFlags.ClientClipChanged);
        var needsRenderContent = needsRender || clientClipChanged;
        var newVisualRect = Rect.Empty;

        if (needsRenderContent)
        {
            _stats.RenderCount++;

            // Render new background (if necessary)
            if (needsRender)
            {
                _renderContext.ClearRenderBuffer();
                RenderHelper.RenderBackground(view, _renderContext);
                view._renderBufferBackground = _renderContext.CloneRenderBuffer(view._renderBufferBackground);
            }
            newVisualRect = newVisualRect.Union(view._renderBufferBackground.MeasureBounds(_measureContext));

            var renderer = view.Render;
            if (renderer is not null)
            {
                // Call user render function
                var clientClip = view.toClient(deviceClip);
                _renderContext.ClearRenderBuffer();
                var renderResult = renderer.Render(view, _renderContext, clientClip);
                _renderContext.FlushClips();
                if (renderResult == RenderResult.Discard)
                    view._renderBuffer = OsRenderBuffer.Empty;
                else if (renderResult != RenderResult.Keep)
                    view._renderBuffer = _renderContext.CloneRenderBuffer(view._renderBuffer);
                if (renderResult != RenderResult.Keep)
                    view._renderResult = renderResult;
                newVisualRect = newVisualRect.Union(view._renderBuffer.MeasureBounds(_measureContext));

                // Call user render over children function
                _renderContext.ClearRenderBuffer();
                var renderResultOver = renderer.RenderOver(view, _renderContext, clientClip);
                _renderContext.FlushClips();
                if (renderResultOver == RenderResult.Discard)
                    view._renderBufferOver = OsRenderBuffer.Empty;
                else if (renderResultOver != RenderResult.Keep)
                    view._renderBufferOver = _renderContext.CloneRenderBuffer(view._renderBufferOver);
                if (renderResultOver != RenderResult.Keep)
                    view._renderResultOver = renderResultOver;
                newVisualRect = newVisualRect.Union(view._renderBufferOver.MeasureBounds(_measureContext));
            }
        }

        // Recurse down the tree if any child needs render or dirty visual rect
        if (view.FlagsChild.HasFlag(ViewFlags.Render)
            || view.FlagsChild.HasFlag(ViewFlags.Moved)
            || view.FlagsChild.HasFlag(ViewFlags.ClientClipChanged))
        {
            foreach (var child in view.Children)
                RenderView(child, deviceClip, ref dirtyRect);
        }

        if (needsRenderContent || view.Flags.HasFlag(ViewFlags.Moved))
        {
            // Dirty the old device rect
            dirtyRect = dirtyRect.Union(view._measureCache.VisualDeviceBoundsAtLastRender.Intersect(deviceClip));

            // Update visual bounds
            if (needsRenderContent)
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


    /// <summary>
    /// Composite the app window into _presentBuffer.
    ///     dirtyRect is the rectangle that needs to be composited
    /// </summary>
    void CompositeAppWindow(Rect dirtyRect)
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
            var view = _appWindow.View;
            CompositeView(view, view.toDevice(view.ContentRect), dirtyRect);
        }
        finally
        {
            if (DIRTY_RECT_ENABLE)
                _presentBuffer.PopClip();
        }
    }

    /// <summary>
    /// Composite the view into _presentBuffer.
    ///     deviceClip is the clipping rectangle of parent controls
    ///     dirtyRect is the rectangle that needs to be composited
    /// </summary>
    void CompositeView(View view, Rect deviceClip, Rect dirtyRect)
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
            if (view.GetProperty(Panel.Clip))
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

            // Composite background and control rendered content
            var presented = false;
            if (!isFullyClipped 
                && (view._renderBuffer.CommandsLength > 0 || view._renderBufferBackground.CommandsLength > 0))
            {
                presented = true;
                _presentBuffer.Composite(view._renderBufferBackground, _stringCache, view.Origin, view.Scale);
                _presentBuffer.Composite(view._renderBuffer, _stringCache, view.Origin, view.Scale);
            }

            // Composite children
            foreach (var child in view.Children)
                CompositeView(child, deviceClip, dirtyRect);

            // Composite control rendered content (over children)
            if (!isFullyClipped && view._renderBufferOver.CommandsLength > 0)
            {
                presented = true;
                _presentBuffer.Composite(view._renderBufferOver, _stringCache, view.Origin, view.Scale);
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
