using System.Diagnostics;
using ZurfurGui.Platform;
using ZurfurGui.Draw;
using ZurfurGui.Base;
using ZurfurGui.Layout;

namespace ZurfurGui.Controls;

/// <summary>
/// A window is a wrapper to hold a component intended to be shown over the main app window.
/// It could be modal or modeless, and could cover the app completely when modal or can
/// be moved by the user when modeless.  The content is displayed inside a client area.
/// </summary>
public partial class Window : Controllable
{
    const string WINDOW_TITLE_NAME = "_windowTitle";
    const string WINDOW_CONTENT_NAME = "_windowContent";
    const string WINDOW_CLOSE_BUTTON_NAME = "_closeButton";

    View _contentView;
    View _titleView;
    View _resizeHandle;

    bool _mouseDown;
    Point _mousePosition;

    public Window()
    {
        InitializeControl();

        _titleView = View.FindByName(WINDOW_TITLE_NAME);
        _contentView = View.FindByName(WINDOW_CONTENT_NAME);
        _resizeHandle = View.FindByName("_resizeHandle");

        // Click on any control brings window to foreground
        View.AddEvent(Zui.PreviewPointerDown, (s, e) =>
        {
            View.BringToFront();
        });

        // Title events
        _titleView.AddEvent(Zui.PreviewPointerDown, _titleView_PreviewPointerDown);
        _titleView.AddEvent(Zui.PreviewPointerMove, _titleView_PreviewPointerMove);
        _titleView.AddEvent(Zui.PointerCaptureLost, (s, e) =>_mouseDown = false);

        _resizeHandle.AddEvent(Zui.PreviewPointerDown, _resizeHandle_PreviewPointerDown);
        _resizeHandle.AddEvent(Zui.PreviewPointerMove, _resizeHandle_PreviewPointerMove);
        _resizeHandle.AddEvent(Zui.PointerCaptureLost, (s, e) => _mouseDown = false);

        var closeButton = _titleView.FindByName(WINDOW_CLOSE_BUTTON_NAME);
        closeButton.AddEvent(Zui.PointerUp, (s, e) => View.Parent?.RemoveChild(View));
    }

    public void LoadContent(Properties[]? contents)
    {
        if (contents != null)
            foreach (var property in contents)
                _contentView.AddChild(Loader.CreateControl(property).View);

        if (Debugger.IsAttached)
            WalkView(View);
    }


    void WalkView(View view, int level = 0)
    {
        Debug.WriteLine($"{new string(' ', level)}{view.Controller.GetType()}: {view.Controller.TypeName}");
        foreach (var child in view.Children)
        {
            WalkView(child, level + 1);
        }
    }


    void _titleView_PreviewPointerDown(object? s, PointerEvent e)
    {
        _mouseDown = true;
        _mousePosition = View.Parent?.toClient(e.Position) ?? new();
        _titleView.CapturePointer = true;
    }

    void _titleView_PreviewPointerMove(object? s, PointerEvent e)
    {
        if (!_mouseDown)
            return;

        var position = View.Parent?.toClient(e.Position) ?? new();
        var diff = position - _mousePosition;
        _mousePosition = position;

        // Update window offset
        View.SetProperty(Zui.Offset, View.GetStyle(Zui.Offset).Or(0) + diff);
        View.InvalidateMeasure(); // TBD: Should be automatic
    }

    void _resizeHandle_PreviewPointerDown(object? s, PointerEvent e)
    {
        _mouseDown = true;
        _mousePosition = View.Parent?.toClient(e.Position) ?? new();
        _resizeHandle.CapturePointer = true;
    }

    void _resizeHandle_PreviewPointerMove(object? s, PointerEvent e)
    {
        if (!_mouseDown)
            return;

        var position = View.Parent?.toClient(e.Position) ?? new();
        var diff = position - _mousePosition;
        _mousePosition = position;

        // Update window offset, clamped SizeMin..SizeMaz
        var size = View.GetStyle(Zui.SizeRequest).Or(View.Size) + diff;
        var sizeMax = View.GetStyle(Zui.SizeMax).Or(double.PositiveInfinity);
        var sizeMin = View.GetStyle(Zui.SizeMin).Or(0).MaxZero;
        View.SetProperty(Zui.SizeRequest, size.Min(sizeMax).Max(sizeMin));
        View.InvalidateMeasure(); // TBD: Should be automatic
    }


    /// <summary>
    /// TBD: Needs to be an actual property
    /// </summary>
    public bool IsWindowWrappingVisible
    {
        get => _titleView.GetProperty(Zui.IsVisible);
        set
        {
            _titleView.SetProperty(Zui.IsVisible, value);
            _resizeHandle.SetProperty(Zui.IsVisible, value);
        }
    }

    /// <summary>
    /// Set the window content
    /// </summary>
    public void SetContent(View content)
    {
        _contentView.ClearChildren();
        _contentView.AddChild(content);
    }


}
