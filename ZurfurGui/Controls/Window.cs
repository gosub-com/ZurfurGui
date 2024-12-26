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
            (Zui.Controller, "Zui.Panel"),
            (Zui.AlignVertical, AlignVertical.Top),
            (Zui.Size, new Size(double.NaN, 24)),
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
            (Zui.Margin, new Thickness(0, 28, 0, 0)),
            (Zui.Content, View.Properties.Get(Zui.Content) ?? [])
        ];

        Properties borderPanel = [
            (Zui.Controller, "Zui.Panel"),
            (Zui.Margin, new Thickness(5)),
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
        context.FillColor = Colors.LightGray;
        var r = new Rect(new(), View.Size);
        context.FillRect(r);
    }

    public bool IsHit(Point point)
    {
        return true;
    }

}
