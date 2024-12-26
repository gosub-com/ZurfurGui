namespace TestApp;

using System.ComponentModel.DataAnnotations;
using ZurfurGui;
using ZurfurGui.Controls;
using ZurfurGui.Render;

public class Main
{
    public static Properties Create(string[] args)
    {
        ControlRegistry.Add(() => { return new FormSearch(); });
        ControlRegistry.Add(() => { return new FormSearch2(); });

        Properties canvas2Controller = [
            (Zui.Controller, "Zui.Panel"),
            (Zui.Margin, new Thickness(10)),
            (Zui.AlignHorizontal, AlignHorizontal.Stretch),
            (Zui.AlignVertical, AlignVertical.Stretch),
            (Zui.Content, (Properties[])[
                [
                    (Zui.Controller, "Zui.QbfWinTest"),
                    (Zui.Margin, new Thickness(115, 175, 0, 0)),
                    (Zui.Size, new Size(400, 500)),
                    (Zui.AlignHorizontal, AlignHorizontal.Left),
                    (Zui.AlignVertical, AlignVertical.Top),
                ], 
                [
                    (Zui.Controller, "Zui.SmallWinTest"),
                    (Zui.Margin, new Thickness(600, 175, 0, 0)),
                    (Zui.Size, new Size(200, 200)),
                    (Zui.AlignHorizontal, AlignHorizontal.Left),
                    (Zui.AlignVertical, AlignVertical.Top),
                ],
                [
                    (Zui.Controller, "Zui.SmallWinTest"),
                    (Zui.Margin, new Thickness(0)),
                    (Zui.Size, new Size(200, 200)),
                    (Zui.AlignHorizontal, AlignHorizontal.Right),
                    (Zui.AlignVertical, AlignVertical.Bottom),
                ],
                [
                    (Zui.Controller, "TestApp.FormSearch"),
                    (Zui.AlignHorizontal, AlignHorizontal.Left),
                    (Zui.AlignVertical, AlignVertical.Top),
                ],
                [
                    (Zui.Controller, "TestApp.FormSearch2"),
                    (Zui.AlignHorizontal, AlignHorizontal.Right),
                    (Zui.AlignVertical, AlignVertical.Top),
                    //(Zui.AlignHorizontal, AlignHorizontal.Stretch),
                    //(Zui.AlignVertical, AlignVertical.Stretch),
                ],
            ]),
        ];

        return canvas2Controller;
    }

}
