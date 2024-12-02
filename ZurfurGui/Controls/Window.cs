using System.Diagnostics;

using ZurfurGui.Render;

namespace ZurfurGui.Controls;

public class Window : Controllable
{
    public string Type => "ZGui.Window";
    public override string ToString() => View.ToString();
    public View View { get; private set; }

    public Window()
    {
        View = new(this);
    }

    public void Build()
    {
        Properties tilePanel = [
            (ZGui.Controller, "ZGui.Panel"),
            (ZGui.AlignVertical, VerticalAlignment.Top),
            (ZGui.Size, new Size(double.NaN, 24)),
            (ZGui.Controls, (Properties[])[
                [
                    (ZGui.Controller, "ZGui.Button"),
                    (ZGui.AlignHorizontal, HorizontalAlignment.Left),
                    (ZGui.Text, "≡"),
                    (ZGui.FontSize, 24.0)
                ],
                [
                    (ZGui.Controller, "ZGui.Button"),
                    (ZGui.AlignHorizontal, HorizontalAlignment.Right),
                    (ZGui.Text, "X"),
                    (ZGui.FontSize, 24.0)
                ],
                [
                    (ZGui.Controller, "ZGui.Label"),
                    (ZGui.DisableHitTest, true),
                    (ZGui.AlignHorizontal, HorizontalAlignment.Center),
                    (ZGui.Text, "Title"),
                    (ZGui.FontSize, 24.0)
                ]
            ])
        ];

        Properties clientPanel = [
            (ZGui.Controller, "ZGui.Panel"),
            (ZGui.Margin, new Thickness(0, 28, 0, 0)),
            (ZGui.Controls, View.Properties.Get(ZGui.Controls) ?? [])
        ];

        Properties borderPanel = [
            (ZGui.Controller, "ZGui.Panel"),
            (ZGui.Margin, new Thickness(5)),
            (ZGui.Controls, (Properties[])[tilePanel, clientPanel])
        ];

        View.Views = [Helper.BuildView(borderPanel)];

        View.Properties.Set(ZGui.PreviewPointerDown, (e) => {
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
