using ZurfurGui.Base;
using ZurfurGui.Controls;
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
        View.AddEvent(Panel.PreviewPointerDown, (s, e) =>
        {
            View.BringToFront();
        });

        // Title events
        _windowTitle.View.AddEvent(Panel.PreviewPointerDown, _windowTitle_PreviewPointerDown);
        _windowTitle.View.AddEvent(Panel.PreviewPointerMove, _windowTitle_PreviewPointerMove);
        _windowTitle.View.AddEvent(Panel.PointerCaptureLost, (s, e) =>_mouseDown = false);

        _resizeHandle.View.AddEvent(Panel.PreviewPointerDown, _resizeHandle_PreviewPointerDown);
        _resizeHandle.View.AddEvent(Panel.PreviewPointerMove, _resizeHandle_PreviewPointerMove);
        _resizeHandle.View.AddEvent(Panel.PointerCaptureLost, (s, e) => _mouseDown = false);

        _closeButton.View.AddEvent(Panel.PointerUp, (s, e) => View.Parent?.RemoveChild(View));
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
        View.SetProperty(Panel.Offset, View.GetStyle(Panel.Offset).Or(0) + diff);
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
        var size = View.GetStyle(Panel.SizeRequest).Or(View.Size) + diff;
        var sizeMax = View.GetStyle(Panel.SizeMax).Or(double.PositiveInfinity);
        var sizeMin = View.GetStyle(Panel.SizeMin).Or(0).MaxZero;
        View.SetProperty(Panel.SizeRequest, size.Min(sizeMax).Max(sizeMin));
    }


    /// <summary>
    /// TBD: Needs to be an actual property
    /// </summary>
    public bool IsWindowWrappingVisible
    {
        get => _windowTitle.View.GetProperty(Panel.IsVisible);
        set
        {
            _windowTitle.View.SetProperty(Panel.IsVisible, value);
            _resizeHandle.View.SetProperty(Panel.IsVisible, value);
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

    public void SetTitle(string title)
    {
        _title.View.SetProperty(TextView.Text, new (title));
    }

}
