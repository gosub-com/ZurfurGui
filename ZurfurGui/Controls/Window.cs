using System.Reflection;
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

    public View BuildView(Properties properties)
    {
        Properties tilePanel = [
            (ZGui.Controller, "ZGui.Panel"),
            (ZGui.AlignVertical, VerticalAlignment.Top),
            (ZGui.Size, new Size(double.NaN, 20)),
            (ZGui.Controls, (Properties[])[
                [
                    (ZGui.Controller, "ZGui.Button"),
                    (ZGui.AlignHorizontal, HorizontalAlignment.Left),
                    (ZGui.Text, "Menu"),
                ],
                [
                    (ZGui.Controller, "ZGui.Button"),
                    (ZGui.AlignHorizontal, HorizontalAlignment.Right),
                    (ZGui.Text, "X")
                ],
                [
                    (ZGui.Controller, "ZGui.Label"),
                    (ZGui.AlignHorizontal, HorizontalAlignment.Center),
                    (ZGui.Text, "Title")
                ]
            ])
        ];

        Properties clientPanel = [
            (ZGui.Controller, "ZGui.Panel"),
            (ZGui.Margin, new Thickness(0, 24, 0, 0)),
            (ZGui.Controls, properties.Get(ZGui.Controls) ?? [])
        ];

        Properties borderPanel = [
            (ZGui.Controller, "ZGui.Panel"),
            (ZGui.Margin, new Thickness(5)),
            (ZGui.Controls, (Properties[])[tilePanel, clientPanel])
        ];

        View.Views.Clear();
        Helper.BuildViewsFromProperties(View.Views, [borderPanel]);
        return View;
    }

    /// <summary>
    /// Same as panel
    /// </summary>
    public Size MeasureView(Size available, MeasureContext measure)
    {
        return Helper.MeasurePanel(View.Views, available, measure);
    }

    /// <summary>
    /// A window puts all controls at (0,0), like a panel.  Position can be controlled using margin.
    /// </summary>
    public Size ArrangeViews(Size final, MeasureContext measure)
    {
        return Helper.ArrangePanel(View.Views, final, measure);
    }

    public void Render(RenderContext context)
    {
        context.FillColor = Colors.LightGray;
        var r = new Rect(new(), View.Size);
        context.FillRect(r);
    }
}
