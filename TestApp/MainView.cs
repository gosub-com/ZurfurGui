namespace TestApp;

using System.ComponentModel.DataAnnotations;
using ZurfurGui;
using ZurfurGui.Controls;
using ZurfurGui.Render;

public class MainView
{
    public static Properties CreateMainView()
    {
        Properties canvas2Controller = [
            (ZGui.Controller, "ZGui.Panel"),
            (ZGui.Margin, new Thickness(10)),
            (ZGui.AlignHorizontal, HorizontalAlignment.Stretch),
            (ZGui.AlignVertical, VerticalAlignment.Stretch),
            (ZGui.Controls, (Properties[])[
                [
                    (ZGui.Controller, "ZGui.QbfWinTest"),
                    (ZGui.Margin, new Thickness(115, 175, 0, 0)),
                    (ZGui.Size, new Size(400, 500)),
                    (ZGui.AlignHorizontal, HorizontalAlignment.Left),
                    (ZGui.AlignVertical, VerticalAlignment.Top),
                ], 
                [
                    (ZGui.Controller, "ZGui.SmallWinTest"),
                    (ZGui.Margin, new Thickness(600, 175, 0, 0)),
                    (ZGui.Size, new Size(200, 200)),
                    (ZGui.AlignHorizontal, HorizontalAlignment.Left),
                    (ZGui.AlignVertical, VerticalAlignment.Top),
                ],
                [
                    (ZGui.Controller, "ZGui.SmallWinTest"),
                    (ZGui.Margin, new Thickness(0)),
                    (ZGui.Size, new Size(200, 200)),
                    (ZGui.AlignHorizontal, HorizontalAlignment.Right),
                    (ZGui.AlignVertical, VerticalAlignment.Bottom),
                ]
            ]),
        ];

        return canvas2Controller;
    }

}
