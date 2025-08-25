using System.Diagnostics;
using ZurfurGui.Platform;
using ZurfurGui.Render;

namespace ZurfurGui.Controls;

/// <summary>
/// A window is a wrapper to hold a component intended to be shown over the main app window.
/// It could be modal or modeless, and could cover the app completely when modal or can
/// be moved by the user when modeless.  The content is displayed inside a client area.
/// </summary>
public class Window : Controllable
{
    public string Type => "Zui.Window";
    public override string ToString() => View.ToString();
    public View View { get; private set; }

    View _contentView;
    View _titleView;

    bool _mouseDown;
    Point _mouseDownOffset;

    public Window()
    {
        View = new(this);

        const string WINDOW_TITLE_NAME = "_windowTitle";
        const string WINDOW_CONTENT_NAME = "_windowContent";

        Properties windowProps = [
            (Zui.Controller, "Zui.DockPanel"),
            (Zui.Content, (Properties[])[
                [
                    (Zui.Controller, "Zui.Border"),
                    (Zui.Name, WINDOW_TITLE_NAME),
                    (Zui.Dock, Dock.Top),
                    (Zui.AlignVertical, AlignVertical.Top),
                    (Zui.Background, Colors.DarkSlateBlue),
                    (Zui.Padding, new Thickness(10,4,10,0)),
                    (Zui.Margin, new Thickness(2)),
                    (Zui.BorderRadius, 10.0),
                    (Zui.Content, (Properties[])[
                        [
                            (Zui.Controller, "Zui.Label"),
                            (Zui.AlignHorizontal, AlignHorizontal.Left),
                            (Zui.Text, "≡"),
                            (Zui.FontSize, 24.0),
                        ],
                        [
                            (Zui.Controller, "Zui.Label"),
                            (Zui.AlignHorizontal, AlignHorizontal.Right),
                            (Zui.Text, "X"),
                            (Zui.FontSize, 24.0)
                        ],
                        [
                            (Zui.Controller, "Zui.Label"),
                            (Zui.DisableHitTest, true),
                            (Zui.AlignHorizontal, AlignHorizontal.Center),
                            (Zui.Text, "Title"),
                            (Zui.FontSize, 24.0)
                        ]
                    ])
                ],
                [
                    (Zui.Controller, "Zui.Panel"),
                    (Zui.Name, WINDOW_CONTENT_NAME),
                    (Zui.Margin, new Thickness(10)),
                ],
            ])
        ];

        var windowView = Helper.BuildView(windowProps);
        View.AddView(windowView);
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
    }

    void _titleView_PreviewPointerDown(object? s, PointerEvent e)
    {
        _mouseDown = true;
        var margin = View.Properties.Get(Zui.Margin);
        _mouseDownOffset = (View.Parent?.toClient(e.Position) ?? new()) - margin.TopLeft;
        _titleView.CapturePointer = true;
    }

    void _titleView_PreviewPointerMove(object? s, PointerEvent e)
    {
        if (!_mouseDown)
            return;

        var position = (View.Parent?.toClient(e.Position) ?? new()) - _mouseDownOffset;
        var margin = View.Properties.Get(Zui.Margin);
        margin.Left = position.X;
        margin.Top = position.Y;
        View.Properties.Set(Zui.Margin, margin);
        View.InvalidateMeasure(); // TBD: Should be automatic
    }

    public void Build()
    {
        var contentProperties = View.Properties.Get(Zui.Content);
        if (contentProperties != null)
            foreach (var property in contentProperties)
                _contentView.AddView(Helper.BuildView(property));
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

    public void Render(RenderContext context)
    {
        var background = View.Properties.Get(Zui.Background, Colors.LightSkyBlue);
        var borderColor = View.Properties.Get(Zui.BorderColor, Colors.AliceBlue);
        var borderRadius = View.Properties.Get(Zui.BorderRadius, 10);
        var borderWidth = View.Properties.Get(Zui.BorderWidth, 2);

        context.FillColor = background;
        var r = new Rect(new(), View.Size);
        context.FillRect(r, borderRadius);
        context.StrokeColor = borderColor;

        r = r.Deflate(borderWidth/2);
        context.LineWidth = borderWidth;
        context.StrokeRect(r, borderRadius);
    }

    public bool IsHit(Point point)
    {
        return true;
    }

}
