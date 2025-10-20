using ZurfurGui.Base;
using ZurfurGui.Draw;
using ZurfurGui.Property;

namespace ZurfurGui.Windows;

public partial class DebugWindow : Controllable
{
    readonly bool UPDATE_ONCE_PER_SECOND = false;

    RenderContext.Stats _prevStats;

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

        var newStats = renderer.RenderContext.RenderStats;
        var _window = renderer.Window;
        var _canvas = renderer.Canvas;
        var canvasDeviceSize = _canvas.DeviceSize;
        var canvasStyleSize = _canvas.StyleSize;
        var textCount = newStats.FillText - _prevStats.FillText;
        var rectCount = newStats.FillRect - _prevStats.FillRect + newStats.StrokeRect - _prevStats.StrokeRect;
        var clipCount = newStats.PushClips - _prevStats.PushClips;
        TextLinesProp text = new([
            $"fps={renderer.Fps}, ms={renderer.AvgMs}, frames={renderer.FrameCount}",
            $"draws={renderer.DrawCount}, measures={renderer.MeasureCount}, styles={renderer.StyleCount}",
            $"DPR={_window.DevicePixelRatio:F2}, CDS={canvasDeviceSize:F2}, CSS={canvasStyleSize:F2}",
            $"WIS={_window.InnerSize}, DPS={_canvas.DevicePixelSize?.ToString() ?? "?"}",
            $"Screen size: ({_window.ScreenSize}), focus={_canvas.HasFocus}",
            $"Text: {textCount}, Rects: {rectCount}, Clips: {clipCount}"
        ]);

        // Don't Invalidate measure
        _statsView.View.SetPropertyNoFlags(Zui.Text, text);
        _statsView.View.InvalidateDraw();
        // _statsView.View.InvalidateMeasure();

        _prevStats = newStats;
    }
}
