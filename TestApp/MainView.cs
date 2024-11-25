namespace TestApp;

using System.ComponentModel.DataAnnotations;
using ZurfurGui;
using ZurfurGui.Controls;
using ZurfurGui.Render;

public class MainView
{
    public static Properties CreateMainView()
    {

        var qbfControl = MakeQbfWin();
        var swtControl1 = MakeSmallWinTest();
        var swtControl2 = MakeSmallWinTest();
        swtControl2.Set(ZGui.Margin, new(10));
        swtControl2.Set(ZGui.AlignHorizontal, HorizontalAlignment.Right);
        swtControl2.Set(ZGui.AlignVertical, VerticalAlignment.Bottom);

        Properties canvas2Controller = [
            (ZGui.Controller, "ZGui.Panel"),
            (ZGui.Margin, new Thickness(10, 10, 10, 10)),
            (ZGui.AlignHorizontal, HorizontalAlignment.Stretch),
            (ZGui.AlignVertical, VerticalAlignment.Stretch),
            (ZGui.Controls, (Properties[])[qbfControl, swtControl1, swtControl2]),
        ];

        return canvas2Controller;
    }

    static Properties MakeSmallWinTest()
    {
        return [
            (ZGui.Controller, "ZGui.Window"),
            (ZGui.Margin, new Thickness(600, 175, 0, 0)),
            (ZGui.Size, new Size(200, 200)),
            (ZGui.AlignHorizontal, HorizontalAlignment.Left),
            (ZGui.AlignVertical, VerticalAlignment.Top),
            (ZGui.Controls, (Properties[])
            [
                [
                    (ZGui.Controller, "ZGui.Button"),
                    (ZGui.Text, "This button is a test"),
                    (ZGui.AlignVertical, VerticalAlignment.Top),
                    (ZGui.AlignHorizontal,  HorizontalAlignment.Left),
                    (ZGui.Size, new Size(100, 100)),
                    (ZGui.Margin, new Thickness(100, 100, 0, 0)),

                ],
                [
                    (ZGui.Controller, "ZGui.Window"),
                    (ZGui.AlignVertical, VerticalAlignment.Top),
                    (ZGui.AlignHorizontal, HorizontalAlignment.Left),
                    (ZGui.Size, new Size(50, 50)),
                    (ZGui.Margin, new Thickness(30, 30, 0, 0)),
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


    static Properties MakeQbfWin()
    {
        return [
            (ZGui.Controller, "ZGui.Window"),
            (ZGui.Margin, new Thickness(115, 175, 0, 0)),
            (ZGui.Size, new Size(400, 500)),
            (ZGui.AlignHorizontal, HorizontalAlignment.Left),
            (ZGui.AlignVertical, VerticalAlignment.Top),
            (ZGui.Controls, (Properties[])[
                [
                    (ZGui.Controller, "ZGui.Column"),
                    (ZGui.Controls, (Properties[])[
                        [
                            (ZGui.Controller, "ZGui.Button"),
                            (ZGui.Text, "ButtonX"),
                            (ZGui.AlignHorizontal, HorizontalAlignment.Center),
                        ],
                        [
                            (ZGui.Controller, "ZGui.Button"),
                            (ZGui.Text, "Button"),
                            (ZGui.AlignHorizontal, HorizontalAlignment.Right),
                        ],
                        [
                            (ZGui.Controller, "ZGui.Button"),
                            (ZGui.Text, "Button"),
                            (ZGui.AlignHorizontal, HorizontalAlignment.Left),
                        ],
                        [
                            (ZGui.Controller, "ZGui.Row"),
                            (ZGui.Controls, (Properties[])[
                                [(ZGui.Controller, "ZGui.Button"), (ZGui.Text, "Button A")],
                                [(ZGui.Controller, "ZGui.Button"), (ZGui.Text, "Button B")],
                                [(ZGui.Controller, "ZGui.Button"), (ZGui.Text, "Button C")],
                            ])
                        ],
                        [
                            (ZGui.Controller, "ZGui.Row"),
                            (ZGui.Controls, (Properties[])[
                                [(ZGui.Controller, "ZGui.Label"), (ZGui.Text, "The")],
                                [(ZGui.Controller, "ZGui.Label"), (ZGui.Text, "Quick Brown Fox Jumps Over")],
                                [(ZGui.Controller, "ZGui.Label"), (ZGui.Text, "The lazy dog")],
                            ])
                        ],
                        [
                            (ZGui.Controller, "ZGui.Row"),
                            (ZGui.Controls, (Properties[])[
                                [(ZGui.Controller, "ZGui.Label"), (ZGui.Text, "The"), (ZGui.FontSize, 16.0)],
                                [(ZGui.Controller, "ZGui.Label"), (ZGui.Text, "Quick"), (ZGui.FontSize, 32.0)],
                                [(ZGui.Controller, "ZGui.Label"), (ZGui.Text, "Brown"), (ZGui.FontSize, 26.0)],
                            ])
                        ],
                        [
                            (ZGui.Controller, "ZGui.Row"),
                            (ZGui.Controls, (Properties[])[
                                [(ZGui.Controller, "ZGui.Label"), (ZGui.Text, "The Quick Brown"), (ZGui.FontSize, 26.0),(ZGui.Margin,new Thickness(-30,0,0,0))],
                                [(ZGui.Controller, "ZGui.Label"), (ZGui.Text, "Fox &Jumps"), (ZGui.FontSize, 26.0)],
                                [(ZGui.Controller, "ZGui.Label"), (ZGui.Text, "Over the lazy dog"), (ZGui.FontSize, 26.0)],
                            ])
                        ],
                        [
                            (ZGui.Controller, "ZGui.Row"),
                            (ZGui.Controls, (Properties[])[
                                [(ZGui.Controller, "ZGui.Label"), (ZGui.Text, "Sans j"), (ZGui.FontSize, 26.0),(ZGui.FontName, "sans-serif")],
                                [(ZGui.Controller, "ZGui.Label"), (ZGui.Text, "Arial j"), (ZGui.FontSize, 26.0),(ZGui.FontName, "Arial")],
                                [(ZGui.Controller, "ZGui.Label"), (ZGui.Text, "﴿█j A﴿│|"), (ZGui.FontSize, 26.0),(ZGui.FontName, "Arial")],
                                [(ZGui.Controller, "ZGui.Label"), (ZGui.Text, "Serif j"), (ZGui.FontSize, 26.0),(ZGui.FontName, "Times New Roman")],
                            ])
                        ],
                        [
                            (ZGui.Controller, "ZGui.Row"),
                            (ZGui.Controls, (Properties[])[
                                [(ZGui.Controller, "ZGui.Label"), (ZGui.Text, "TheQuickBrown"), (ZGui.FontSize, 26.0)],
                                [(ZGui.Controller, "ZGui.Label"), (ZGui.Text, " "), (ZGui.FontSize, 26.0)],
                                [(ZGui.Controller, "ZGui.Label"), (ZGui.Text, "    "), (ZGui.FontSize, 26.0)],
                            ])
                        ],
                        [
                            (ZGui.Controller, "ZGui.Row"),
                            (ZGui.Controls, (Properties[])[
                                [(ZGui.Controller, "ZGui.Label"), (ZGui.Text, "1 The Quick Brown"), (ZGui.FontSize, 40.0),(ZGui.FontName, "Times New ROman")],
                                [(ZGui.Controller, "ZGui.Label"), (ZGui.Text, "2 Fox Jumps Over"), (ZGui.FontSize, 26.0)],
                                [(ZGui.Controller, "ZGui.Label"), (ZGui.Text, "3 The Lazy Dog"), (ZGui.FontSize, 26.0)],
                                [(ZGui.Controller, "ZGui.Label"), (ZGui.Text, "4 And a Cow"), (ZGui.FontSize, 26.0)],
                                [(ZGui.Controller, "ZGui.Label"), (ZGui.Text, "5 And a Zebra"), (ZGui.FontSize, 16.0),(ZGui.AlignVertical, VerticalAlignment.Center)],
                                [(ZGui.Controller, "ZGui.Label"), (ZGui.Text, ""), (ZGui.FontSize, 26.0)],
                                [(ZGui.Controller, "ZGui.Label"), (ZGui.Text, ""), (ZGui.FontSize, 26.0)],
                            ])
                        ],
                        [
                            (ZGui.Controller, "ZGui.Row"),
                            (ZGui.Wrap, true),
                            (ZGui.Controls, (Properties[])[
                                [(ZGui.Controller, "ZGui.Label"), (ZGui.Text, "The Quick Brown"), (ZGui.FontSize, 40.0),(ZGui.FontName, "Times New Roman")],
                                [(ZGui.Controller, "ZGui.Label"), (ZGui.Text, "Fox Jumps Over"), (ZGui.FontSize, 26.0)],
                                [(ZGui.Controller, "ZGui.Label"), (ZGui.Text, "The Lazy Dog"), (ZGui.FontSize, 26.0)],
                                [(ZGui.Controller, "ZGui.Label"), (ZGui.Text, "And a Cow"), (ZGui.FontSize, 32.0),(ZGui.AlignVertical, VerticalAlignment.Center)],
                                [(ZGui.Controller, "ZGui.Label"), (ZGui.Text, "Center"), (ZGui.FontSize, 16.0),(ZGui.AlignVertical, VerticalAlignment.Center)],
                                [(ZGui.Controller, "ZGui.Label"), (ZGui.Text, "Top"), (ZGui.FontSize, 16.0),(ZGui.AlignVertical, VerticalAlignment.Top)],
                                [(ZGui.Controller, "ZGui.Label"), (ZGui.Text, "Bottom"), (ZGui.FontSize, 16.0),(ZGui.AlignVertical, VerticalAlignment.Bottom)],
                                [(ZGui.Controller, "ZGui.Label"), (ZGui.Text, "Stretch"), (ZGui.FontSize, 16.0),(ZGui.AlignVertical, VerticalAlignment.Stretch)],
                            ])
                        ],
                        [
                            (ZGui.Controller, "ZGui.Row"),
                            (ZGui.Controls, (Properties[])[
                                [(ZGui.Controller, "ZGui.Label"), (ZGui.Text, "Done - row")],
                            ])
                        ],
                        [
                            (ZGui.Controller, "ZGui.Label"),
                            (ZGui.Text, "Done - Label"),
                        ],
                        [
                            (ZGui.Controller, "ZGui.Button"),
                            (ZGui.Text, "Done - Button"),
                        ],
                    ])
                ]
            ])
        ];
    }


}
