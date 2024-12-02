using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Controls;
using ZurfurGui.Render;

namespace ZurfurGui.Components;

/// <summary>
/// Quick brown fox test
/// </summary>
internal class QbfWinTest : Controllable
{
    public string Type => "ZGui.QbfWinTest";

    public View View { get; private set; }

    public QbfWinTest()
    {
        View = new(this);
    }

    public void Build()
    {
        View.Views = [Helper.BuildView(MakeQbfWinTest())];
    }

    static Properties MakeQbfWinTest()
    {
        return [
            (ZGui.Controller, "ZGui.Window"),
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
                                [(ZGui.Controller, "ZGui.Label"), (ZGui.Text, "﴿█j A﴿│|☰"), (ZGui.FontSize, 26.0),(ZGui.FontName, "Arial")],
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
