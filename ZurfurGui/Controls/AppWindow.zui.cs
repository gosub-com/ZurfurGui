using System.Diagnostics;
using ZurfurGui.Base;
using ZurfurGui.Render;
using ZurfurGui.Property;

namespace ZurfurGui.Controls;

/// <summary>
/// The AppWindow is either the main browser window, or a canvas inside a web page,
/// or the computer desktop when running as a native applications.   It is the root 
/// of the view tree and holds global app state.
/// 
/// It holds all the other windows:
///     MainAppView: The background app contained in the browser, canvas, or desktop application.
///     FloatingWindows: Modal and modeless windows that can be moved around by the user.
///     
/// Use View.AppWindow to get access to this object.
/// </summary>
public partial class AppWindow : Controllable, Drawable
{
    readonly bool DRAW_TEST_PATTERN = false;

    public string DrawType => "AppWindow";
    public Renderer? Renderer { get; private set; }
    internal PointerOver? PointerHover { get; private set; }

    /// <summary>
    /// Triggered before rendering each frame
    /// </summary>
    public event EventHandler? PreRenderFrame;
    internal void CallPreRenderFrame() => PreRenderFrame?.Invoke(this, EventArgs.Empty);

    public AppWindow()
    {
        InitializeControl();
        View.Draw = this;
    }


    /// <summary>
    /// Set the main app window
    /// </summary>
    public void SetMainappWindow(Controllable control)
    {
        _mainAppView.View.ClearChildren();
        _mainAppView.View.AddChild(control.View);
    }

    /// <summary>
    /// Show a new modeless dialog window over the main app window
    /// </summary>
    public void ShowWindow(Window window)
    {
        _floatingWindows.View.AddChild(window.View);
    }

    /// <summary>
    /// Show a control inside a window container.
    /// TBD: Some of these properties should represent initial location settings, then they dissapear.
    /// </summary>
    public Window ShowWindow(Controllable control, 
        PointProp? location = null,  AlignProp ?align = null, ThicknessProp? margin = null, SizeProp? sizeRequst = null)
    {
        var window = new Window();
        window.View.SetProperty(Zui.Align, align ?? new(AlignHorizontal.Left, AlignVertical.Top));
        if (location != null)
            window.View.SetProperty(Zui.Offset, location.Value);
        if (margin != null)
            window.View.SetProperty(Zui.Margin, margin.Value);
        if (sizeRequst != null)
            window.View.SetProperty(Zui.SizeRequest, sizeRequst.Value);

        window.LoadContent([control]);
        _floatingWindows.View.AddChild(window.View);
        return window;
    }


    public bool IsDarkMode
    {
        get => View.GetProperty(Zui.IsDarkMode).Or(false);
        set => View.SetProperty(Zui.IsDarkMode, value);
    }

    /// <summary>
    /// Called by renderer so we can display some stats
    /// </summary>
    internal void SetAppWindowGlobals(Renderer renderer, PointerOver pointerHover)
    {
        Renderer = renderer;
        PointerHover = pointerHover;
    }

    public bool IsHit(View view, Point point)
    {
        return false;
    }

    public void Draw(View view, RenderContext context)
    {
        if (!DRAW_TEST_PATTERN)
            return;

        var left = 15;
        var top = 50;
        var ls = 26;

        // Draw background numbers to check sizing
        context.FillColor = new Color(0x80, 0x80, 0xF0);
        context.FontName = "sans-serif";
        context.FontSize = 16;
        for (var i = 0; i < 30; i++)
        {
            for (var j = 0; j < 30; j++)
            {
                var x = i * 50;
                var y = j * 20;
                context.FillText($"{x / 10},{y / 10}", x, y);
            }
        }

        // Big "M.M."
        context.FontName = "sans-serif";
        context.FontSize = 500;
        context.FillText($"M.M.", left, top + 17 * ls);

        // Squares to compare with Big "M.M."
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
    }

}
