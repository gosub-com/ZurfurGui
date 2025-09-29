using ZurfurGui.Base;
using ZurfurGui.Draw;
using ZurfurGui.Property;

namespace ZurfurGui.Windows;

public partial class DebugWindow : Controllable
{
    View _statsView;
    RenderContext.Stats _stats;

    public DebugWindow()
    {
        InitializeControl();

        _statsView = View.FindByName("_statsView");
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

        var newStats = renderer.RenderContext.RenderStats;
        var _window = renderer.Window;
        var _canvas = renderer.Canvas;
        var canvasDeviceSize = _canvas.DeviceSize;
        var canvasStyleSize = _canvas.StyleSize;
        var textCount = newStats.FillText - _stats.FillText;
        var rectCount = newStats.FillRect - _stats.FillRect + newStats.StrokeRect - _stats.StrokeRect;
        var clipCount = newStats.PushClips - _stats.PushClips;
        _statsView.SetProperty(Zui.Text, new ([
            $"fps={renderer.Fps}, ms={renderer.AvgMs}, count={renderer.FrameCount}",
            $"DPR={_window.DevicePixelRatio:F2}, CDS={canvasDeviceSize:F2}, CSS={canvasStyleSize:F2}",
            $"WIS={_window.InnerSize}, DPS={_canvas.DevicePixelSize?.ToString() ?? "?"}",
            $"Screen size: ({_window.ScreenSize}), focus={_canvas.HasFocus}",
            $"Text: {textCount}, Rects: {rectCount}, Clips: {clipCount}"
        ]));
        _stats = newStats;
    }
}
