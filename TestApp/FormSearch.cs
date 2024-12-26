using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui;
using ZurfurGui.Controls;
using ZurfurGui.Render;

namespace TestApp;

internal class FormSearch : Controllable
{
    public string Type => "TestApp.FormSearch";

    public View View { get; private set; }

    public FormSearch()
    {
        View = new(this);
    }

    public void Build()
    {
        View.Views = [Helper.BuildView(MakeSearchForm())];
    }

    static Properties MakeSearchForm()
    {
        return [
            (Zui.Controller, "Zui.Border"),
            (Zui.Content, (Properties[])
            [
                [
                    (Zui.Controller, "Zui.Column"),
                    (Zui.Content, (Properties[])
                    [
                        [
                            (Zui.Controller, "Zui.Button"),
                            (Zui.Text, "Future Search Form Title"),
                            (Zui.AlignHorizontal, AlignHorizontal.Center),
                        ],
                        [
                            (Zui.Controller, "Zui.TextBox"),
                            (Zui.Text, "Test")
                        ],
                        [
                            (Zui.Controller, "Zui.Button"),
                            (Zui.Text, "Button"),
                            (Zui.AlignHorizontal, AlignHorizontal.Right),
                        ],
                        [
                            (Zui.Controller, "Zui.Border"),
                            (Zui.Content, (Properties[])
                            [
                                [
                                    (Zui.Controller, "Zui.Label"),
                                    (Zui.Text, "Text in border"),
                                ],
                            ])
                        ],
                        [
                            (Zui.Controller, "Zui.Border"),
                            (Zui.Content, (Properties[])
                            [
                                [
                                    (Zui.Controller, "Zui.Button"),
                                    (Zui.Text, "Button in border"),
                                ],
                            ])
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
                        ]
                    ])
                ]
            ])
        ];
    }

}
