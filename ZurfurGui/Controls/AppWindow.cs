using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Platform;
using ZurfurGui.Render;

namespace ZurfurGui.Controls;

/// <summary>
/// The AppWindow is either the main browser window, or a canvas inside a web page,
/// or the computer desktop when running as a native applications.  It holds all the 
/// other windows:
///     MainAppView: The background app contained in the browser, canvas, or desktop application.
///     FloatingWindows: Modeless windows that can be moved around by the user.
///     DialogWindows: Modal windows that can be shown over the main app view.
/// </summary>
public class AppWindow : Controllable
{
    readonly bool DRAW_NUMBERS = false;

    OsWindow? _window;
    OsCanvas? _canvas;

    int _frameCount;
    int _fps;
    long _avgMs;

    public string Component => "Zui.AppWindow";
    public override string ToString() => View.ToString();
    public View View { get; private set; }

    readonly View _mainAppView;
    readonly View _floatingWindows;
    readonly View _dialogWindows;

    internal List<View> _pointerCaptureList = new();


    public AppWindow()
    {
        View = new(this);

        _mainAppView = new Panel().View;
        _mainAppView.Properties.Set(Zui.Name, "_mainAppView");
        _floatingWindows = new Panel().View;
        _floatingWindows.Properties.Set(Zui.Name, "_floatingWindows");
        _dialogWindows = new Panel().View;
        _dialogWindows.Properties.Set(Zui.Name, "_dialogWindows");

        View.AddViews([_mainAppView, _floatingWindows, _dialogWindows]);
    }

    public void SetMainappWindow(Controllable control)
    {
        View.InvalidateMeasure(); // TBD: Move to AddView
        _mainAppView.ClearViews();
        _mainAppView.AddView(control.View);
    }

    /// <summary>
    /// Show a new modeless dialog window over the main app window
    /// </summary>
    public void ShowWindow(Window window)
    {
        View.InvalidateMeasure(); // TBD: Move to AddView
        _floatingWindows.AddView(window.View);
    }

    /// <summary>
    /// Called by renderer so we can display some stats
    /// </summary>
    internal void SetRenderStats(int count, int fps, int avgMs)
    {
        _fps = fps;
        _avgMs = avgMs;
        _frameCount = count;
    }

    /// <summary>
    /// Called by renderer so we can display some stats
    /// </summary>
    internal void SetRenderWindow(OsWindow window, OsCanvas canvas)
    {
        _window = window;
        _canvas = canvas;
    }

    public void Render(RenderContext renderContext) 
    {
        //return;
        var left = 15;
        var top = 50;
        var ls = 26;

        renderContext.FillColor = new Color(0x20, 0x20, 0x80);
        renderContext.FillRect(0, 0, View.Size.Width, View.Size.Height);

        renderContext.FillColor = new Color(0x80, 0x80, 0xF0);
        renderContext.FontName = "sans-serif";
        renderContext.FontSize = 16;

        if (DRAW_NUMBERS)
        {
            for (var i = 0; i < 30; i++)
            {
                for (var j = 0; j < 30; j++)
                {
                    var x = i * 50;
                    var y = j * 20;
                    renderContext.FillText($"{x / 10},{y / 10}", x, y);
                }
            }
        }

        // Big "M.M"
        renderContext.FontName = "sans-serif";
        renderContext.FontSize = 500;
        renderContext.FillText($"M.M.", left, top + 17 * ls);

        // Show stats
        renderContext.FontName = "sans-serif";
        renderContext.FontSize = 26;
        renderContext.FillColor = new Color(0xC0, 0xC0, 0xF0);
        if (_window != null && _canvas != null)
        {
            var canvasDeviceSize = _canvas.DeviceSize;
            var canvasStyleSize = _canvas.StyleSize;
            renderContext.FillText($"|WPR={_window.DevicePixelRatio:F2}", left, top + 1 * ls);
            renderContext.FillText($"|CDS={canvasDeviceSize:F2}, CSS={canvasStyleSize:F2}", left, top + 2 * ls);
            renderContext.FillText($"│WIS={_window.InnerSize}, COS={_canvas.DevicePixelSize?.ToString() ?? "?"}", left, top + 3 * ls);
            renderContext.FillText($"│Screen size: ({_window.ScreenSize}), foucs={_canvas.HasFocus}", left, top + 4 * ls);
        }

        // Squares
        var sx = 500;
        renderContext.LineWidth = 1;
        renderContext.StrokeColor = Colors.Red;
        renderContext.StrokeRect(10 + sx, 10, 50, 50);

        renderContext.LineWidth = 2;
        renderContext.StrokeColor = Colors.Green;
        renderContext.StrokeRect(60 + sx, 60, 50, 50);

        renderContext.LineWidth = 3;
        renderContext.StrokeColor = Colors.Blue;
        renderContext.StrokeRect(110 + sx, 110, 50, 50);

        renderContext.LineWidth = 8;
        renderContext.StrokeColor = new Color(255, 255, 0);
        renderContext.StrokeRect(160 + sx, 60, 50, 50);

        renderContext.LineWidth = 16;
        renderContext.StrokeColor = new Color(255, 0, 255);
        renderContext.StrokeRect(210 + sx, 10, 50, 50);

        // More stats
        renderContext.FontName = "sans-serif";
        renderContext.FontSize = 26;
        renderContext.FillColor = new Color(0xC0, 0xC0, 0xF0);
        renderContext.FillText($"|fps={_fps}, ms={_avgMs}, count={_frameCount}", left, top + 0 * ls);
    }

    internal bool GetIsPointerCaptured(View view)
    {
        return _pointerCaptureList.Contains(view);
    }
    internal void SetIsPointerCapture(View view, bool capture)
    {
        Debug.WriteLine($"Capture {capture}");
        var i = _pointerCaptureList.IndexOf(view);
        if (capture)
        {
            // TBD: Throw if not in pointer down
            if (i < 0)
                _pointerCaptureList.Add(view);
        }
        if (!capture)
        {
            if (i >= 0)
            {
                _pointerCaptureList.RemoveAt(i);
                view.Properties.Get(Zui.PointerCaptureLost)?.Invoke(view, EventArgs.Empty);
            }
        }
    }

    internal void ClearPointerCaptureList()
    {
        Debug.WriteLine("CaptureLost");
        var c = _pointerCaptureList;
        _pointerCaptureList = new();
        foreach (var view in c)
            view.Properties.Get(Zui.PointerCaptureLost)?.Invoke(view, EventArgs.Empty);
    }

}
