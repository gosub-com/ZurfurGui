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
    double _renderMs = 0;
    double _drawMs = 0;

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
                _totalMs = (s.TotalMs - _prevRenderStatsOneSecond.TotalMs) / _fps;
                _renderMs = (s.RenderMs - _prevRenderStatsOneSecond.RenderMs) / _fps;
                _drawMs = (s.DrawMs - _prevRenderStatsOneSecond.DrawMs) / _fps;
            }
            else
            {
                _totalMs = 0;
                _renderMs = 0;
                _drawMs = 0;
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
            $"FPS={_fps}, ms={_totalMs:F1} ({_renderMs:F1}, {_drawMs:F1}), Frames={stats.FrameCount}",
            $"Measures={rStatsDiff.InvalidMeasureCount}, Draws={rStatsDiff.InvalidDrawCount}, Buffer={stats.DrawBufferLength}",
            //$"DPR={_window.DevicePixelRatio:F2}, CDS={canvasDeviceSize:F2}, CSS={canvasStyleSize:F2}",
            //$"WIS={_window.InnerSize}, DPS={_canvas.DevicePixelSize?.ToString() ?? "?"}",
            //$"Screen size: ({_window.ScreenSize}), focus={_canvas.HasFocus}",
            $"Text: {cStatsDiff.FillText}, Rects: {cStatsDiff.FillRect + cStatsDiff.StrokeRect}, Clips: {cStatsDiff.PushClips}",
            _gc,
        ];

        // Don't Invalidate measure
        _statsView.View.SetPropertyNoFlags(TextView.TextProperty, text);
        _statsView.View.InvalidateDraw();
        //_statsView.View.InvalidateMeasure();
        _prevRenderStats = renderer.Stats;

        _prevContextStatsFrame = cStatsNew;
    }
}
