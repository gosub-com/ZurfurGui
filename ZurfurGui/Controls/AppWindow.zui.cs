using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Platform;
using ZurfurGui.Draw;
using ZurfurGui.Base;

namespace ZurfurGui.Controls;

/// <summary>
/// The AppWindow is either the main browser window, or a canvas inside a web page,
/// or the computer desktop when running as a native applications.  It holds all the 
/// other windows:
///     MainAppView: The background app contained in the browser, canvas, or desktop application.
///     FloatingWindows: Modal and modeless windows that can be moved around by the user.
/// </summary>
public partial class AppWindow : Controllable, Drawable
{
    readonly bool DRAW_NUMBERS = false;

    OsWindow? _window;
    OsCanvas? _canvas;

    int _frameCount;
    int _fps;
    long _avgMs;

    readonly View _mainAppView;
    readonly View _floatingWindows;

    internal List<View> _pointerCaptureList = new();

    public string DrawType => "AppWindow";

    public Drawable DefaultDraw => this;


    public AppWindow()
    {
        InitializeControl();

        // TBD: Should be code generated
        _mainAppView = View.FindByName("_mainAppView") ?? throw new Exception("Missing _mainAppView");
        _floatingWindows = View.FindByName("_floatingWindows") ?? throw new Exception("Missing _floatingWindows");
    }


    public void SetMainappWindow(Controllable control)
    {
        View.InvalidateMeasure(); // TBD: Move to AddView
        _mainAppView.ClearChildren();
        _mainAppView.AddChild(control.View);
    }

    /// <summary>
    /// Show a new modeless dialog window over the main app window
    /// </summary>
    public void ShowWindow(Window window)
    {
        View.InvalidateMeasure(); // TBD: Move to AddView
        _floatingWindows.AddChild(window.View);
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

    public bool IsHit(View view, Point point)
    {
        return false;
    }

    public void Draw(View view, DrawContext context, Rect contentRect)
    {
        var left = 15;
        var top = 50;
        var ls = 26;

        context.FillColor = new Color(0x20, 0x20, 0x80);
        context.FillRect(0, 0, View.Size.Width, View.Size.Height);

        context.FillColor = new Color(0x80, 0x80, 0xF0);
        context.FontName = "sans-serif";
        context.FontSize = 16;

        if (DRAW_NUMBERS)
        {
            for (var i = 0; i < 30; i++)
            {
                for (var j = 0; j < 30; j++)
                {
                    var x = i * 50;
                    var y = j * 20;
                    context.FillText($"{x / 10},{y / 10}", x, y);
                }
            }
        }

        // Big "M.M"
        context.FontName = "sans-serif";
        context.FontSize = 500;
        context.FillText($"M.M.", left, top + 17 * ls);

        // Show stats
        context.FontName = "sans-serif";
        context.FontSize = 26;
        context.FillColor = new Color(0xC0, 0xC0, 0xF0);
        if (_window != null && _canvas != null)
        {
            var canvasDeviceSize = _canvas.DeviceSize;
            var canvasStyleSize = _canvas.StyleSize;
            context.FillText($"|WPR={_window.DevicePixelRatio:F2}", left, top + 1 * ls);
            context.FillText($"|CDS={canvasDeviceSize:F2}, CSS={canvasStyleSize:F2}", left, top + 2 * ls);
            context.FillText($"│WIS={_window.InnerSize}, COS={_canvas.DevicePixelSize?.ToString() ?? "?"}", left, top + 3 * ls);
            context.FillText($"│Screen size: ({_window.ScreenSize}), foucs={_canvas.HasFocus}", left, top + 4 * ls);
        }

        // Squares
        var sx = 500;
        context.LineWidth = 1;
        context.StrokeColor = Colors.Red;
        context.StrokeRect(10 + sx, 10, 50, 50);

        context.LineWidth = 2;
        context.StrokeColor = Colors.Green;
        context.StrokeRect(60 + sx, 60, 50, 50);

        context.LineWidth = 3;
        context.StrokeColor = Colors.Blue;
        context.StrokeRect(110 + sx, 110, 50, 50);

        context.LineWidth = 8;
        context.StrokeColor = new Color(255, 255, 0);
        context.StrokeRect(160 + sx, 60, 50, 50);

        context.LineWidth = 16;
        context.StrokeColor = new Color(255, 0, 255);
        context.StrokeRect(210 + sx, 10, 50, 50);

        // More stats
        context.FontName = "sans-serif";
        context.FontSize = 26;
        context.FillColor = new Color(0xC0, 0xC0, 0xF0);
        context.FillText($"|fps={_fps}, ms={_avgMs}, count={_frameCount}", left, top + 0 * ls);
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
                view.GetProperty(Zui.PointerCaptureLost)?.Invoke(view, EventArgs.Empty);
            }
        }
    }

    internal void ClearPointerCaptureList()
    {
        Debug.WriteLine("CaptureLost");
        var c = _pointerCaptureList;
        _pointerCaptureList = new();
        foreach (var view in c)
            view.GetProperty(Zui.PointerCaptureLost)?.Invoke(view, EventArgs.Empty);
    }

}
