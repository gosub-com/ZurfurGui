
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui;
using ZurfurGui.Controls;
using ZurfurGui.Render;

namespace TestApp;

internal class FormSearch2 : Controllable
{
    public string Type => "TestApp.FormSearch2";

    public View View { get; private set; }

    public FormSearch2()
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
                            (Zui.Controller, "Zui.DockPanel"),
                            (Zui.Content, (Properties[])
                            [
                                [
                                    (Zui.Controller, "Zui.Button"),
                                    (Zui.Text, "[X]"),
                                    (Zui.Dock, Dock.Right)
                                ],
                                [
                                    (Zui.Controller, "Zui.Label"),
                                    (Zui.Margin, new Thickness(0, 0, 10, 0)),
                                    (Zui.Text, "[X] Replace"),
                                    (Zui.Dock, Dock.Right)
                                ],
                                [
                                    (Zui.Controller, "Zui.Label"),
                                    (Zui.Text, "Search Form:"),
                                    (Zui.Dock, Dock.Left),
                                ]
                            ])
                        ],
                        [
                            (Zui.Controller, "Zui.DockPanel"),
                            (Zui.Content, (Properties[])
                            [
                                [
                                    (Zui.Controller, "Zui.Button"),
                                    (Zui.Text, "Find (F3)"),
                                    (Zui.Size, new Size(120, double.NaN)),
                                    (Zui.Dock, Dock.Right),
                                    (Zui.Margin, new Thickness(10, 0, 0,0))
                                ],
                                [
                                    (Zui.Controller, "Zui.TextBox"),
                                    (Zui.Text, "Search Text"),
                                    (Zui.Dock, Dock.Left),
                                ]
                            ])
                        ],
                        [
                            (Zui.Controller, "Zui.DockPanel"),
                            (Zui.Content, (Properties[])
                            [
                                [
                                    (Zui.Controller, "Zui.Label"),
                                    (Zui.Text, "Matches (#)"),
                                    (Zui.Size, new Size(120, double.NaN)),
                                    (Zui.Dock, Dock.Right),
                                ],
                                [
                                    (Zui.Controller, "Zui.Label"),
                                    (Zui.Text, "Replace with:"),
                                ]
                            ])
                        ],
                        [
                            (Zui.Controller, "Zui.DockPanel"),
                            (Zui.Content, (Properties[])
                            [
                                [
                                    (Zui.Controller, "Zui.Button"),
                                    (Zui.Text, "Replace"),
                                    (Zui.Size, new Size(120, double.NaN)),
                                    (Zui.Margin, new Thickness(10, 0, 0, 0)),
                                    (Zui.Dock, Dock.Right),
                                ],
                                [
                                    (Zui.Controller, "Zui.TextBox"),
                                    (Zui.Text, "Replace Text"),
                                ]
                            ])
                        ],
                        [
                            (Zui.Controller, "Zui.DockPanel"),
                            (Zui.Content, (Properties[])
                            [
                                [
                                    (Zui.Controller, "Zui.Button"),
                                    (Zui.Text, "Replace All"),
                                    (Zui.Size, new Size(120, double.NaN)),
                                    (Zui.Margin, new Thickness(10, 0, 0, 0)),
                                    (Zui.Dock, Dock.Right),
                                ],
                                [
                                    (Zui.Controller, "Zui.Label"),
                                    (Zui.Text, "[X] Match Case"),
                                ],
                                [
                                    (Zui.Controller, "Zui.Label"),
                                    (Zui.Text, "[X] Match Whole Word"),
                                    (Zui.Margin, new Thickness(10, 0, 0, 0)),
                                ]
                            ])
                        ],
                    ])
                ]
            ])
        ];
    }

}
