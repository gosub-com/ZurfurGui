using System.Diagnostics;
using ZurfurGui.Platform;
using ZurfurGui.Draw;

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

    bool _mouseDown;
    Point _mousePosition;

    public Window()
    {
        InitializeComponent();

        _titleView = View.FindByName(WINDOW_TITLE_NAME);
        _contentView = View.FindByName(WINDOW_CONTENT_NAME);

        // Click on any control brings window to foreground
        View.Properties.AddEvent(Zui.PreviewPointerDown, (s, e) =>
        {
            View.BringToFront();
        });

        // Title events
        _titleView.Properties.AddEvent(Zui.PreviewPointerDown, _titleView_PreviewPointerDown);
        _titleView.Properties.AddEvent(Zui.PreviewPointerMove, _titleView_PreviewPointerMove);
        _titleView.Properties.AddEvent(Zui.PointerCaptureLost, (s, e) =>_mouseDown = false);

        var closeButton = _titleView.FindByName(WINDOW_CLOSE_BUTTON_NAME);
        closeButton.Properties.AddEvent(Zui.PointerUp, (s, e) => View.Parent?.RemoveView(View));
    }

    public void LoadContent(Properties[]? contents)
    {
        if (contents != null)
            foreach (var property in contents)
                _contentView.AddView(Loader.CreateControl(property).View);

        if (Debugger.IsAttached)
            WalkView(View);
    }


    void WalkView(View view, int level = 0)
    {
        Debug.WriteLine($"{new string(' ', level)}{view.Control.GetType()}: {view.Control.Component}");
        foreach (var child in view.Views)
        {
            WalkView(child, level + 1);
        }
    }


    void _titleView_PreviewPointerDown(object? s, PointerEvent e)
    {
        _mouseDown = true;
        var margin = View.Properties.Get(Zui.Margin);
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
        View.Properties.Set(Zui.Offset, View.Properties.Get(Zui.Offset) + diff);


        View.InvalidateMeasure(); // TBD: Should be automatic
    }

    /// <summary>
    /// TBD: Needs to be an actual property
    /// </summary>
    public bool IsWindowWrappingVisible
    {
        get => _titleView.Properties.Get(Zui.IsVisible, true);
        set => _titleView.Properties.Set(Zui.IsVisible, value);
    }

    /// <summary>
    /// Set the window content
    /// </summary>
    public void SetContent(View content)
    {
        _contentView.ClearViews();
        _contentView.AddView(content);
    }


}
