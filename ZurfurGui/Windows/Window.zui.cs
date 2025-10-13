using System.Diagnostics;
using ZurfurGui.Base;
using ZurfurGui.Platform;
using ZurfurGui.Property;

namespace ZurfurGui.Windows;

/// <summary>
/// A window is a wrapper to hold a component intended to be shown over the main app window.
/// It could be modal or modeless, and could cover the app completely when modal or can
/// be moved by the user when modeless.  The content is displayed inside a client area.
/// </summary>
public partial class Window : Controllable
{
    bool _mouseDown;
    Point _mousePosition;

    public Window()
    {
        InitializeControl();

        // Click on any control brings window to foreground
        View.AddEvent(Zui.PreviewPointerDown, (s, e) =>
        {
            View.BringToFront();
        });

        // Title events
        _windowTitle.View.AddEvent(Zui.PreviewPointerDown, _windowTitle_PreviewPointerDown);
        _windowTitle.View.AddEvent(Zui.PreviewPointerMove, _windowTitle_PreviewPointerMove);
        _windowTitle.View.AddEvent(Zui.PointerCaptureLost, (s, e) =>_mouseDown = false);

        _resizeHandle.View.AddEvent(Zui.PreviewPointerDown, _resizeHandle_PreviewPointerDown);
        _resizeHandle.View.AddEvent(Zui.PreviewPointerMove, _resizeHandle_PreviewPointerMove);
        _resizeHandle.View.AddEvent(Zui.PointerCaptureLost, (s, e) => _mouseDown = false);

        _closeButton.View.AddEvent(Zui.PointerUp, (s, e) => View.Parent?.RemoveChild(View));
    }

    public void LoadContent(Properties[]? contents)
    {
        if (contents != null)
            foreach (var property in contents)
                _windowContent.View.AddChild(Loader.CreateControl(property).View);
    }

    public void LoadContent(Controllable[] contents)
    {
        foreach (var control in contents)
            _windowContent.View.AddChild(control.View);
    }


    void _windowTitle_PreviewPointerDown(object? s, PointerEvent e)
    {
        _mouseDown = true;
        _mousePosition = View.Parent?.toClient(e.Position) ?? new();
        _windowTitle.View.CapturePointer = true;
    }

    void _windowTitle_PreviewPointerMove(object? s, PointerEvent e)
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
        _resizeHandle.View.CapturePointer = true;
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
        get => _windowTitle.View.GetProperty(Zui.IsVisible).Or(true);
        set
        {
            _windowTitle.View.SetProperty(Zui.IsVisible, value);
            _resizeHandle.View.SetProperty(Zui.IsVisible, value);
        }
    }

    /// <summary>
    /// Set the window content
    /// </summary>
    public void SetContent(View content)
    {
        _windowContent.View.ClearChildren();
        _windowContent.View.AddChild(content);
    }


}
