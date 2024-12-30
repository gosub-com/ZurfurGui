using System.Diagnostics;

using ZurfurGui.Render;

namespace ZurfurGui.Controls;

public class Window : Controllable
{
    public string Type => "Zui.Window";
    public override string ToString() => View.ToString();
    public View View { get; private set; }

    public Window()
    {
        View = new(this);
    }

    public void Build()
    {
        Properties tilePanel = [
            (Zui.Controller, "Zui.Border"),
            (Zui.Dock, Dock.Top),
            (Zui.AlignVertical, AlignVertical.Top),
            (Zui.Background, Colors.DarkSlateBlue),
            (Zui.Padding, new Thickness(7)),
            (Zui.Margin, new Thickness(2)),
            (Zui.BorderRadius, 10.0),
            (Zui.Content, (Properties[])[
                [
                    (Zui.Controller, "Zui.Button"),
                    (Zui.AlignHorizontal, AlignHorizontal.Left),
                    (Zui.Text, "≡"),
                    (Zui.FontSize, 24.0)
                ],
                [
                    (Zui.Controller, "Zui.Button"),
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
        ];

        Properties clientPanel = [
            (Zui.Controller, "Zui.Panel"),
            (Zui.Margin, new Thickness(10)),
            (Zui.Content, View.Properties.Get(Zui.Content) ?? [])
        ];

        Properties borderPanel = [
            (Zui.Controller, "Zui.DockPanel"),
            //(Zui.Margin, new Thickness(5)),
            (Zui.Content, (Properties[])[tilePanel, clientPanel])
        ];

        View.Views = [Helper.BuildView(borderPanel)];

        View.Properties.Set(Zui.PreviewPointerDown, (e) => {
            Debug.WriteLine("Window down");

            // TBD: Remove this, enforce windows at top level
            View.Parent?.BringToFront();
        });

    }

    public void Render(RenderContext context)
    {
        context.FillColor = Colors.LightSkyBlue;
        var r = new Rect(new(), View.Size);
        context.FillRect(r, 10);
        context.StrokeColor = Colors.AliceBlue;
        r = r.Deflate(1);
        context.LineWidth = 2;
        context.StrokeRect(r, 10);
    }

    public bool IsHit(Point point)
    {
        return true;
    }

}
