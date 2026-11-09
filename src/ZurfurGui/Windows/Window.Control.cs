using ZurfurGui.Base;
using ZurfurGui.Controls;
using ZurfurGui.Input;
using ZurfurGui.Property;

namespace ZurfurGui.Windows;

/// <summary>
/// A window is a wrapper to hold a component intended to be shown over the main app window.
/// It could be modal or modeless, and could cover the app completely when modal or can
/// be moved by the user when modeless.  The content is displayed inside a client area.
/// </summary>
public partial class Window : Controllable
{
    Point _mousePosition;


    /// <summary>
    /// Initializes a window and registers its move, resize, and close interactions.
    /// </summary>
    public Window()
    {
        InitializeControl();

        // Click on any control brings window to foreground
        View.AddEvent(Panel.PreviewPointerDown, (s, e) =>
        {
            View.BringToFront();
        });

        // Drag window events
        var dragPan = _dragWindow.View.AddGestureRecognizer(new PanRecognizer(NewPanOptions()));
        dragPan.PanStarted += PanStarted;
        dragPan.PanMoved += _dragWindow_PanMoved;

        var resizePan = _resizeHandle.View.AddGestureRecognizer(new PanRecognizer(NewPanOptions()));
        resizePan.PanStarted += PanStarted;
        resizePan.PanMoved += _resizeHandle_PanMoved;

        _closeButton.View.AddGestureRecognizer(
            new ClickRecognizer((s, e) => View.RemoveFromParent()));
    }

    /// <summary>
    /// Receives parent-supplied content in the _windowContent.
    /// The loader adds parent-supplied content to ContentHost, so this control only needs to provide the host.
    /// </summary>
    public View ContentHost => _windowContent.View;


    /// <summary>
    /// Set the window contents to the given Controllable's View. 
    /// </summary>
    public void LoadControl(Controllable control)
    {
        _windowContent.View.ClearChildren();
        _windowContent.View.AddChild(control.View);
    }


    // Pan options for window drag and resize: left mouse button, no flick, immediate start
    static PanOptions NewPanOptions()
    {
        return new PanOptions { MouseButtons = PointerButtons.Left, Flick = false, Slop = 0 };
    }

    // Remember where the pan started, in parent coordinates
    void PanStarted(object? s, PanEventArgs e)
    {
        _mousePosition = View.Parent?.toClient(e.Contact.DevicePosition) ?? new();
    }

    // Move the window as the title bar is panned
    void _dragWindow_PanMoved(object? s, PanEventArgs e)
    {
        var position = View.Parent?.toClient(e.Contact.DevicePosition) ?? new();
        var diff = position - _mousePosition;
        _mousePosition = position;

        // Update window position
        View.SetProperty(Panel.Offset, View.GetProperty(Panel.Offset).Or(0) + diff);
    }

    // Resize the window as the handle is panned
    void _resizeHandle_PanMoved(object? s, PanEventArgs e)
    {
        var position = View.Parent?.toClient(e.Contact.DevicePosition) ?? new();
        var diff = position - _mousePosition;
        _mousePosition = position;

        // Update window offset
        var size = View.GetProperty(Panel.SizeRequest).Or(View.Size) + diff;
        var sizeMax = View.GetProperty(Panel.SizeMax).Or(double.PositiveInfinity);
        var sizeMin = View.GetProperty(Panel.SizeMin).Or(0).MaxZero;
        View.SetProperty(Panel.SizeRequest, size.Min(sizeMax).Max(sizeMin));
    }


    /// <summary>
    /// TBD: Needs to be an actual property
    /// </summary>
    public bool IsWindowWrappingVisible
    {
        get => _windowTitle.View.IsVisible;
        set
        {
            _windowTitle.View.IsVisible = value;
            _resizeHandle.View.IsVisible = value;
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

    /// <summary>
    /// Set the window title
    /// </summary>
    public void SetTitle(string title)
    {
        _title.DataContext.Text = new TextLines([title]);
    }

}
