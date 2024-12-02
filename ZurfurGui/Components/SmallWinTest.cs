using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using ZurfurGui.Controls;
using ZurfurGui.Render;

namespace ZurfurGui.Components;

internal class SmallWinTest : Controllable
{
    public string Type => "ZGui.SmallWinTest";

    public View View { get; private set; }

    public SmallWinTest()
    {
        View = new(this);
    }

    public void Build()
    {
        View.Views = [Helper.BuildView(MakeSmallWinTest())];

        var v = View.FindByName("button2");
        v.Properties.Set(ZGui.PointerDown, (e) => {
            Debug.WriteLine("Button2 mouse down");
        });
        v.Properties.Set(ZGui.PointerUp, (e) => {
            Debug.WriteLine("Button2 mouse up");
        });

    }

    static Properties MakeSmallWinTest()
    {
        return [
            (ZGui.Controller, "ZGui.Window"),
            (ZGui.Controls, (Properties[])
            [
                [
                    (ZGui.Controller, "ZGui.Button"),
                    (ZGui.Name, "button1"),
                    (ZGui.Text, "Big Button Test"),
                    (ZGui.AlignVertical, VerticalAlignment.Top),
                    (ZGui.AlignHorizontal,  HorizontalAlignment.Left),
                    (ZGui.Size, new Size(100, 100)),
                    (ZGui.Margin, new Thickness(100, 100, 0, 0)),
                ],
                [
                    (ZGui.Controller, "ZGui.Button"),
                    (ZGui.Name, "button2"),
                    (ZGui.Text, "Test 2"),
                    (ZGui.AlignVertical, VerticalAlignment.Top),
                    (ZGui.AlignHorizontal,  HorizontalAlignment.Left),
                    (ZGui.Margin, new Thickness(10, 20, 0, 0)),
                ],
                [
                    (ZGui.Controller, "ZGui.Button"),
                    (ZGui.Name, "button3"),
                    (ZGui.Text, "Test 3"),
                    (ZGui.AlignVertical, VerticalAlignment.Top),
                    (ZGui.AlignHorizontal,  HorizontalAlignment.Left),
                    (ZGui.Margin, new Thickness(10, 50, 0, 0)),

                ],
                [
                    (ZGui.Controller, "ZGui.Label"),
                    (ZGui.Text, "TC"),
                    (ZGui.AlignVertical, VerticalAlignment.Top),
                    (ZGui.AlignHorizontal, HorizontalAlignment.Center),
                ],
                [
                    (ZGui.Controller, "ZGui.Label"),
                    (ZGui.Text, "TS"),
                    (ZGui.AlignVertical, VerticalAlignment.Top),
                    (ZGui.AlignHorizontal, HorizontalAlignment.Stretch),
                ],
                [
                    (ZGui.Controller, "ZGui.Label"),
                    (ZGui.Text, "TR"),
                    (ZGui.AlignVertical, VerticalAlignment.Top),
                    (ZGui.AlignHorizontal, HorizontalAlignment.Right),
                ],
                [
                    (ZGui.Controller, "ZGui.Label"),
                    (ZGui.Text, "TL"),
                    (ZGui.AlignVertical, VerticalAlignment.Top),
                    (ZGui.AlignHorizontal, HorizontalAlignment.Left),
                ],
                [
                    (ZGui.Controller, "ZGui.Label"),
                    (ZGui.Text, "Middle"),
                    (ZGui.AlignVertical, VerticalAlignment.Center),
                    (ZGui.AlignHorizontal, HorizontalAlignment.Center),
                ],
                [
                    (ZGui.Controller, "ZGui.Label"),
                    (ZGui.Text, "CS"),
                    (ZGui.AlignVertical, VerticalAlignment.Center),
                    (ZGui.AlignHorizontal, HorizontalAlignment.Stretch),
                ],
                [
                    (ZGui.Controller, "ZGui.Label"),
                    (ZGui.Text, "CR"),
                    (ZGui.AlignVertical, VerticalAlignment.Center),
                    (ZGui.AlignHorizontal, HorizontalAlignment.Right),
                ],
                [
                    (ZGui.Controller, "ZGui.Label"),
                    (ZGui.Text, "CL"),
                    (ZGui.AlignVertical, VerticalAlignment.Center),
                    (ZGui.AlignHorizontal, HorizontalAlignment.Left),
                ],
                [
                    (ZGui.Controller, "ZGui.Label"),
                    (ZGui.Text, "BC"),
                    (ZGui.AlignVertical, VerticalAlignment.Bottom),
                    (ZGui.AlignHorizontal, HorizontalAlignment.Center),
                ],
                [
                    (ZGui.Controller, "ZGui.Label"),
                    (ZGui.Text, "BS"),
                    (ZGui.AlignVertical, VerticalAlignment.Bottom),
                    (ZGui.AlignHorizontal, HorizontalAlignment.Stretch),
                ],
                [
                    (ZGui.Controller, "ZGui.Label"),
                    (ZGui.Text, "BR"),
                    (ZGui.AlignVertical, VerticalAlignment.Bottom),
                    (ZGui.AlignHorizontal, HorizontalAlignment.Right),
                ],
                [
                    (ZGui.Controller, "ZGui.Label"),
                    (ZGui.Text, "BL"),
                    (ZGui.AlignVertical, VerticalAlignment.Bottom),
                    (ZGui.AlignHorizontal, HorizontalAlignment.Left),
                ]
            ])
        ];
    }

}
