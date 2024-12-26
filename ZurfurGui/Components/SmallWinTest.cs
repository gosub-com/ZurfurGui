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
    public string Type => "Zui.SmallWinTest";

    public View View { get; private set; }

    public SmallWinTest()
    {
        View = new(this);
    }

    public void Build()
    {
        View.Views = [Helper.BuildView(MakeSmallWinTest())];

        var v = View.FindByName("button2");
        v.Properties.Set(Zui.PointerDown, (e) => {
            Debug.WriteLine("Button2 mouse down");
        });
        v.Properties.Set(Zui.PointerUp, (e) => {
            Debug.WriteLine("Button2 mouse up");
        });

    }

    static Properties MakeSmallWinTest()
    {
        return [
            (Zui.Controller, "Zui.Window"),
            (Zui.Content, (Properties[])
            [
                [
                    (Zui.Controller, "Zui.Button"),
                    (Zui.Name, "button1"),
                    (Zui.Text, "Big Button Test"),
                    (Zui.AlignVertical, AlignVertical.Top),
                    (Zui.AlignHorizontal,  AlignHorizontal.Left),
                    (Zui.Size, new Size(100, 100)),
                    (Zui.Margin, new Thickness(100, 100, 0, 0)),
                ],
                [
                    (Zui.Controller, "Zui.Button"),
                    (Zui.Name, "button2"),
                    (Zui.Text, "Test 2"),
                    (Zui.AlignVertical, AlignVertical.Top),
                    (Zui.AlignHorizontal,  AlignHorizontal.Left),
                    (Zui.Margin, new Thickness(10, 20, 0, 0)),
                ],
                [
                    (Zui.Controller, "Zui.Button"),
                    (Zui.Name, "button3"),
                    (Zui.Text, "Test 3"),
                    (Zui.AlignVertical, AlignVertical.Top),
                    (Zui.AlignHorizontal,  AlignHorizontal.Left),
                    (Zui.Margin, new Thickness(10, 50, 0, 0)),

                ],
                [
                    (Zui.Controller, "Zui.Label"),
                    (Zui.Text, "TC"),
                    (Zui.AlignVertical, AlignVertical.Top),
                    (Zui.AlignHorizontal, AlignHorizontal.Center),
                ],
                [
                    (Zui.Controller, "Zui.Label"),
                    (Zui.Text, "TS"),
                    (Zui.AlignVertical, AlignVertical.Top),
                    (Zui.AlignHorizontal, AlignHorizontal.Stretch),
                ],
                [
                    (Zui.Controller, "Zui.Label"),
                    (Zui.Text, "TR"),
                    (Zui.AlignVertical, AlignVertical.Top),
                    (Zui.AlignHorizontal, AlignHorizontal.Right),
                ],
                [
                    (Zui.Controller, "Zui.Label"),
                    (Zui.Text, "TL"),
                    (Zui.AlignVertical, AlignVertical.Top),
                    (Zui.AlignHorizontal, AlignHorizontal.Left),
                ],
                [
                    (Zui.Controller, "Zui.Label"),
                    (Zui.Text, "Middle"),
                    (Zui.AlignVertical, AlignVertical.Center),
                    (Zui.AlignHorizontal, AlignHorizontal.Center),
                ],
                [
                    (Zui.Controller, "Zui.Label"),
                    (Zui.Text, "CS"),
                    (Zui.AlignVertical, AlignVertical.Center),
                    (Zui.AlignHorizontal, AlignHorizontal.Stretch),
                ],
                [
                    (Zui.Controller, "Zui.Label"),
                    (Zui.Text, "CR"),
                    (Zui.AlignVertical, AlignVertical.Center),
                    (Zui.AlignHorizontal, AlignHorizontal.Right),
                ],
                [
                    (Zui.Controller, "Zui.Label"),
                    (Zui.Text, "CL"),
                    (Zui.AlignVertical, AlignVertical.Center),
                    (Zui.AlignHorizontal, AlignHorizontal.Left),
                ],
                [
                    (Zui.Controller, "Zui.Label"),
                    (Zui.Text, "BC"),
                    (Zui.AlignVertical, AlignVertical.Bottom),
                    (Zui.AlignHorizontal, AlignHorizontal.Center),
                ],
                [
                    (Zui.Controller, "Zui.Label"),
                    (Zui.Text, "BS"),
                    (Zui.AlignVertical, AlignVertical.Bottom),
                    (Zui.AlignHorizontal, AlignHorizontal.Stretch),
                ],
                [
                    (Zui.Controller, "Zui.Label"),
                    (Zui.Text, "BR"),
                    (Zui.AlignVertical, AlignVertical.Bottom),
                    (Zui.AlignHorizontal, AlignHorizontal.Right),
                ],
                [
                    (Zui.Controller, "Zui.Label"),
                    (Zui.Text, "BL"),
                    (Zui.AlignVertical, AlignVertical.Bottom),
                    (Zui.AlignHorizontal, AlignHorizontal.Left),
                ]
            ])
        ];
    }

}
