using ZurfurGui.Base;
using ZurfurGui.Controls;
using ZurfurGui.Render;

namespace ZurfurGui.Windows;

public partial class DebugWindow : Controllable
{
    readonly bool UPDATE_ONCE_PER_SECOND = false;

    RenderContext.RenderContextStats _prevContextStatsFrame;
    Renderer.RendererStats _prevRenderStatsOneSecond;
    Renderer.RendererStats _prevRenderStats;

    string _gc = "";
    double _fps = 0;
    double _totalMs = 0;
    double _measureMs = 0;
    double _renderMs = 0;
    double _compositeMs = 0;
    double _presentMs = 0;

    public DebugWindow()
    {
        InitializeControl();
    }


    public void OnAttach()
    {
        View.AppWindow!.PreRenderFrame += DebugWindow_PreRenderFrame;
    }
    public void OnDetach()
    {
        View.AppWindow!.PreRenderFrame -= DebugWindow_PreRenderFrame;
    }

    private void DebugWindow_PreRenderFrame(object? sender, EventArgs e)
    {
        if (View.AppWindow?.Renderer is not { } renderer)
            return;

        if (UPDATE_ONCE_PER_SECOND && !renderer.FpsUpdatedOnceASecond)
            return;

        // Update some stats once per second
        if (renderer.FpsUpdatedOnceASecond)
        {
            _gc = $"GC: {GC.CollectionCount(2)}, {GC.CollectionCount(1)}, {GC.CollectionCount(0)}, "
               + $"Mem={GC.GetTotalMemory(false) / 1024 / 1024} MB";

            var s = renderer.Stats;
            _fps = (int)(s.FrameCount - _prevRenderStatsOneSecond.FrameCount);
            if (_fps > 0) 
            {
                var diff = s - _prevRenderStatsOneSecond;

                _totalMs = diff.TotalMs / _fps;
                _measureMs = diff.MeasureMs / _fps;
                _renderMs = diff.RenderMs / _fps;
                _compositeMs = diff.CompositeMs / _fps;
                _presentMs = diff.PresentMs / _fps;
            }
            else
            {
                _totalMs = 0;
                _measureMs = 0;
                _renderMs = 0;
                _compositeMs = 0;
                _presentMs = 0;
            }
            _prevRenderStatsOneSecond = s;
        }


        // Fixup the render stats so we totals based on per frame
        var stats = renderer.Stats;
        var cStatsNew = renderer.RenderContextStats;
        var cStatsDiff = cStatsNew - _prevContextStatsFrame;
        var _window = renderer.Window;
        var _canvas = renderer.Canvas;
        var canvasDeviceSize = _canvas.DeviceSize;
        var canvasStyleSize = _canvas.StyleSize;
        var rStatsDiff = stats - _prevRenderStats;
        TextLines text = [
            $"{stats.FrameCount}: {_fps}, ms={_totalMs:F1} ({_measureMs:F2}, {_renderMs:F2}, {_compositeMs:F2}, {_presentMs:F2})",
            $"Measures={rStatsDiff.MeasureCount}, Renders={rStatsDiff.RenderCount}, Composites={rStatsDiff.CompositeCount}", 
            $"Buffer={stats.PresentBufferLength}",
            //$"DPR={_window.DevicePixelRatio:F2}, CDS={canvasDeviceSize:F2}, CSS={canvasStyleSize:F2}",
            //$"WIS={_window.InnerSize}, DPS={_canvas.DevicePixelSize?.ToString() ?? "?"}",
            //$"Screen size: ({_window.ScreenSize}), focus={_canvas.HasFocus}",
            $"Text: {cStatsDiff.FillText}, Rects: {cStatsDiff.FillRect + cStatsDiff.StrokeRect}, Clips: {cStatsDiff.PushClips}",
            _gc,
        ];

        _statsView.DataContext.Text = text;
        _prevRenderStats = renderer.Stats;

        _prevContextStatsFrame = cStatsNew;
    }
}
