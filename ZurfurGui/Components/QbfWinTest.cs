using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Controls;

namespace ZurfurGui.Components;

/// <summary>
/// Quick brown fox test
/// </summary>
public class QbfWinTest : Controllable
{
    public string Type => "Zui.QbfWinTest";

    public View View { get; private set; }

    public QbfWinTest()
    {
        View = new(this);
        View.AddView(Helper.BuildView(MakeQbfWinTest()));
    }

    public void Build()
    {
    }

    static Properties MakeQbfWinTest()
    {
        return [
            (Zui.Controller, "Zui.Column"),
            (Zui.Content, (Properties[])[
                [
                    (Zui.Controller, "Zui.Button"),
                    (Zui.Text, "ButtonX"),
                    (Zui.AlignHorizontal, AlignHorizontal.Center),
                ],
                [
                    (Zui.Controller, "Zui.Button"),
                    (Zui.Text, "Button"),
                    (Zui.AlignHorizontal, AlignHorizontal.Right),
                ],
                [
                    (Zui.Controller, "Zui.Button"),
                    (Zui.Text, "Button"),
                    (Zui.AlignHorizontal, AlignHorizontal.Left),
                ],
                [
                    (Zui.Controller, "Zui.Row"),
                    (Zui.Content, (Properties[])[
                        [(Zui.Controller, "Zui.Button"), (Zui.Text, "Button A")],
                        [(Zui.Controller, "Zui.Button"), (Zui.Text, "Button B")],
                        [(Zui.Controller, "Zui.Button"), (Zui.Text, "Button C")],
                    ])
                ],
                [
                    (Zui.Controller, "Zui.Row"),
                    (Zui.Content, (Properties[])[
                        [(Zui.Controller, "Zui.Label"), (Zui.Text, "The")],
                        [(Zui.Controller, "Zui.Label"), (Zui.Text, "Quick Brown Fox Jumps Over")],
                        [(Zui.Controller, "Zui.Label"), (Zui.Text, "The lazy dog")],
                    ])
                ],
                [
                    (Zui.Controller, "Zui.Row"),
                    (Zui.Content, (Properties[])[
                        [(Zui.Controller, "Zui.Label"), (Zui.Text, "The"), (Zui.FontSize, 16.0)],
                        [(Zui.Controller, "Zui.Label"), (Zui.Text, "Quick"), (Zui.FontSize, 32.0)],
                        [(Zui.Controller, "Zui.Label"), (Zui.Text, "Brown"), (Zui.FontSize, 26.0)],
                    ])
                ],
                [
                    (Zui.Controller, "Zui.Row"),
                    (Zui.Content, (Properties[])[
                        [(Zui.Controller, "Zui.Label"), (Zui.Text, "The Quick Brown"), (Zui.FontSize, 26.0),(Zui.Margin,new Thickness(-30,0,0,0))],
                        [(Zui.Controller, "Zui.Label"), (Zui.Text, "Fox &Jumps"), (Zui.FontSize, 26.0)],
                        [(Zui.Controller, "Zui.Label"), (Zui.Text, "Over the lazy dog"), (Zui.FontSize, 26.0)],
                    ])
                ],
                [
                    (Zui.Controller, "Zui.Row"),
                    (Zui.Content, (Properties[])[
                        [(Zui.Controller, "Zui.Label"), (Zui.Text, "Sans j"), (Zui.FontSize, 26.0),(Zui.FontName, "sans-serif")],
                        [(Zui.Controller, "Zui.Label"), (Zui.Text, "Arial j"), (Zui.FontSize, 26.0),(Zui.FontName, "Arial")],
                        [(Zui.Controller, "Zui.Label"), (Zui.Text, "﴿█j A﴿│|☰"), (Zui.FontSize, 26.0),(Zui.FontName, "Arial")],
                        [(Zui.Controller, "Zui.Label"), (Zui.Text, "Serif j"), (Zui.FontSize, 26.0),(Zui.FontName, "Times New Roman")],
                    ])
                ],
                [
                    (Zui.Controller, "Zui.Row"),
                    (Zui.Content, (Properties[])[
                        [(Zui.Controller, "Zui.Label"), (Zui.Text, "TheQuickBrown"), (Zui.FontSize, 26.0)],
                        [(Zui.Controller, "Zui.Label"), (Zui.Text, " "), (Zui.FontSize, 26.0)],
                        [(Zui.Controller, "Zui.Label"), (Zui.Text, "    "), (Zui.FontSize, 26.0)],
                    ])
                ],
                [
                    (Zui.Controller, "Zui.Row"),
                    (Zui.Content, (Properties[])[
                        [(Zui.Controller, "Zui.Label"), (Zui.Text, "1 The Quick Brown"), (Zui.FontSize, 40.0),(Zui.FontName, "Times New ROman")],
                        [(Zui.Controller, "Zui.Label"), (Zui.Text, "2 Fox Jumps Over"), (Zui.FontSize, 26.0)],
                        [(Zui.Controller, "Zui.Label"), (Zui.Text, "3 The Lazy Dog"), (Zui.FontSize, 26.0)],
                        [(Zui.Controller, "Zui.Label"), (Zui.Text, "4 And a Cow"), (Zui.FontSize, 26.0)],
                        [(Zui.Controller, "Zui.Label"), (Zui.Text, "5 And a Zebra"), (Zui.FontSize, 16.0),(Zui.AlignVertical, AlignVertical.Center)],
                        [(Zui.Controller, "Zui.Label"), (Zui.Text, ""), (Zui.FontSize, 26.0)],
                        [(Zui.Controller, "Zui.Label"), (Zui.Text, ""), (Zui.FontSize, 26.0)],
                    ])
                ],
                [
                    (Zui.Controller, "Zui.Row"),
                    (Zui.Wrap, true),
                    (Zui.Content, (Properties[])[
                        [(Zui.Controller, "Zui.Label"), (Zui.Text, "The Quick Brown"), (Zui.FontSize, 40.0),(Zui.FontName, "Times New Roman")],
                        [(Zui.Controller, "Zui.Label"), (Zui.Text, "Fox Jumps Over"), (Zui.FontSize, 26.0)],
                        [(Zui.Controller, "Zui.Label"), (Zui.Text, "The Lazy Dog"), (Zui.FontSize, 26.0)],
                        [(Zui.Controller, "Zui.Label"), (Zui.Text, "And a Cow"), (Zui.FontSize, 32.0),(Zui.AlignVertical, AlignVertical.Center)],
                        [(Zui.Controller, "Zui.Label"), (Zui.Text, "Center"), (Zui.FontSize, 16.0),(Zui.AlignVertical, AlignVertical.Center)],
                        [(Zui.Controller, "Zui.Label"), (Zui.Text, "Top"), (Zui.FontSize, 16.0),(Zui.AlignVertical, AlignVertical.Top)],
                        [(Zui.Controller, "Zui.Label"), (Zui.Text, "Bottom"), (Zui.FontSize, 16.0),(Zui.AlignVertical, AlignVertical.Bottom)],
                        [(Zui.Controller, "Zui.Label"), (Zui.Text, "Stretch"), (Zui.FontSize, 16.0),(Zui.AlignVertical, AlignVertical.Stretch)],
                    ])
                ],
                [
                    (Zui.Controller, "Zui.Row"),
                    (Zui.Content, (Properties[])[
                        [(Zui.Controller, "Zui.Label"), (Zui.Text, "Done - row")],
                    ])
                ],
                [
                    (Zui.Controller, "Zui.Label"),
                    (Zui.Text, "Done - Label"),
                ],
                [
                    (Zui.Controller, "Zui.Button"),
                    (Zui.Text, "Done - Button"),
                ],
            ])
        ];
    }

}
