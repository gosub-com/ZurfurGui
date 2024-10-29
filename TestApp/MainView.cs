namespace TestApp;

using ZurfurGui;
using ZurfurGui.Controls;


public class MainView
{
    public static IEnumerable<Controllable> CreateView()
    {
        var smallWin = MakeSmallWinTest();

        var smallWinBR = MakeSmallWinTest();
        smallWinBR.View.Control?.Properties.Set(View.Margin, new(10));
        smallWinBR.View.Control?.Properties.Set(View.AlignHorizontal, HorizontalAlignment.Right);
        smallWinBR.View.Control?.Properties.Set(View.AlignVertical, VerticalAlignment.Bottom);

        var canvas = new Canvas(){
            Properties = [
                (View.Margin, new Thickness(10, 10, 10, 10)),
                (View.AlignHorizontal, HorizontalAlignment.Stretch),
                (View.AlignVertical, VerticalAlignment.Stretch),
            ],
        };

        return [MakeQbfWin(), smallWin, canvas, smallWinBR];
    }

    static Window MakeSmallWinTest()
    {
        return new Window
        {
            Properties = [
                (View.Margin, new Thickness(600, 175, 0, 0)),
                (View.Size, new Size(150, 150)),
                (View.AlignHorizontal, HorizontalAlignment.Left),
                (View.AlignVertical, VerticalAlignment.Top),
            ],

            Controls = [
                new Button {
                    Properties = [
                        (View.Text, "This button is a test"),
                        (View.AlignVertical, VerticalAlignment.Top),
                        (View.AlignHorizontal,  HorizontalAlignment.Left),
                        (View.Size, new Size(100, 100)),
                        (View.Margin, new Thickness(100, 100, 0, 0)),
                    ],
                },

                new Window {
                    Properties = [
                        (View.AlignVertical, VerticalAlignment.Top),
                        (View.AlignHorizontal, HorizontalAlignment.Left),
                        (View.Size, new Size(50, 50)),
                        (View.Margin, new Thickness(30, 30, 0, 0)),
                    ],
                    Controls = [new Canvas()]
                },
                new Label {
                    Properties = [
                        (View.Text, "TC"),
                        (View.AlignVertical, VerticalAlignment.Top),
                        (View.AlignHorizontal, HorizontalAlignment.Center),
                    ]
                },
                new Label {
                    Properties = [
                        (View.Text, "TS"),
                        (View.AlignVertical, VerticalAlignment.Top),
                        (View.AlignHorizontal, HorizontalAlignment.Stretch),
                    ]
                },
                new Label {
                    Properties = [
                        (View.Text, "TR"),
                        (View.AlignVertical, VerticalAlignment.Top),
                        (View.AlignHorizontal, HorizontalAlignment.Right),
                    ]
                },
                new Label {
                    Properties = [
                        (View.Text, "TL"),
                        (View.AlignVertical, VerticalAlignment.Top),
                        (View.AlignHorizontal, HorizontalAlignment.Left),
                    ]
                },
                new Label {
                    Properties = [
                        (View.Text, "Middle"),
                        (View.AlignVertical, VerticalAlignment.Center),
                        (View.AlignHorizontal, HorizontalAlignment.Center),
                    ]
                },
                new Label {
                    Properties = [
                        (View.Text, "CS"),
                        (View.AlignVertical, VerticalAlignment.Center),
                        (View.AlignHorizontal, HorizontalAlignment.Stretch),
                    ]
                },
                new Label {
                    Properties = [
                        (View.Text, "CR"),
                        (View.AlignVertical, VerticalAlignment.Center),
                        (View.AlignHorizontal, HorizontalAlignment.Right),
                    ]
                },
                new Label {
                    Properties = [
                        (View.Text, "CL"),
                        (View.AlignVertical, VerticalAlignment.Center),
                        (View.AlignHorizontal, HorizontalAlignment.Left),
                    ]
                },
                new Label {
                    Properties = [
                        (View.Text, "BC"),
                        (View.AlignVertical, VerticalAlignment.Bottom),
                        (View.AlignHorizontal, HorizontalAlignment.Center),
                    ]
                },
                new Label {
                    Properties = [
                        (View.Text, "BS"),
                        (View.AlignVertical, VerticalAlignment.Bottom),
                        (View.AlignHorizontal, HorizontalAlignment.Stretch),
                    ]
                },
                new Label {
                    Properties = [
                        (View.Text, "BR"),
                        (View.AlignVertical, VerticalAlignment.Bottom),
                        (View.AlignHorizontal, HorizontalAlignment.Right),
                    ]
                },
                new Label {
                    Properties = [
                        (View.Text, "BL"),
                        (View.AlignVertical, VerticalAlignment.Bottom),
                        (View.AlignHorizontal, HorizontalAlignment.Left),
                    ]
                },
            ]
        };
    }

    static Window MakeQbfWin()
    {
        return new Window
        {
            Properties = [
                (View.Margin, new Thickness(115, 175, 0, 0)),
                (View.Size, new Size(400, 500)),
                (View.AlignHorizontal, HorizontalAlignment.Left),
                (View.AlignVertical, VerticalAlignment.Top),
            ],

            Controls = [
                new Column {
                    Controls = [
                        new Button {
                            Properties = [
                                (View.Text, "Button"),
                                (View.AlignHorizontal, HorizontalAlignment.Center),
                            ],
                        },
                        new Button {
                            Properties = [
                                (View.Text, "Button"),
                                (View.AlignHorizontal, HorizontalAlignment.Right),
                            ],
                        },
                        new Button {
                            Properties = [
                                (View.Text, "Button"),
                                (View.AlignHorizontal, HorizontalAlignment.Left),
                            ],
                        },
                        new Row {
                            Controls = [
                                new Button { Properties = [(View.Text, "Button A")]},
                                new Button { Properties = [(View.Text, "Button B")]},
                                new Button { Properties = [(View.Text, "Button C")]},
                            ]
                        },
                        new Row {
                            Controls = [
                                new Label { Properties = [(View.Text, "The")]},
                                new Label { Properties = [(View.Text, "Quick Brown Fox Jumps Over")]},
                                new Label { Properties = [(View.Text, "The lazy dog")]},
                            ]
                        },
                        new Row {
                            Controls = [
                                new Label { Properties = [(View.Text, "The"), (Label.FontSize, 16.0)]},
                                new Label { Properties = [(View.Text, "Quick"), (Label.FontSize, 32.0)]},
                                new Label { Properties = [(View.Text, "Brown"), (Label.FontSize, 26.0)]},
                            ]
                        },
                        new Row {
                            Controls = [
                                new Label {
                                    Properties = [
                                        (View.Text, "The Quick Brown"),
                                        (Label.FontSize, 26.0),
                                        (View.Margin, new Thickness(-30, 0, 0, 0)),
                                    ],
                                },
                                new Label {
                                    Properties = [
                                        (View.Text, "Fox &Jumps"),
                                        (Label.FontSize, 26.0),
                                    ],
                                },
                                new Label {
                                    Properties = [
                                        (View.Text, "Over the lazy dog"),
                                        (Label.FontSize, 26.0),
                                    ],
                                },
                            ]
                        },
                        new Row {
                            Controls = [
                                new Label {
                                    Properties = [
                                        (View.Text, "Sans j"),
                                        (Label.FontSize, 26.0),
                                        (Label.FontName, "sans-serif"),
                                    ],
                                },
                                new Label {
                                    Properties = [
                                        (View.Text, "Arial j"),
                                        (Label.FontSize, 26.0),
                                        (Label.FontName, "Arial"),
                                    ],
                                },
                                new Label {
                                    Properties = [
                                        (View.Text, "﴿█j A﴿│|"),
                                        (Label.FontSize, 26.0),
                                        (Label.FontName, "Arial"),
                                    ],
                                },
                                new Label {
                                    Properties = [
                                        (View.Text, "Serif j"),
                                        (Label.FontSize, 26.0),
                                        (Label.FontName, "Times New Roman"),
                                    ],
                                },
                            ]
                        },
                        new Row {
                            Controls = [
                                new Label {
                                    Properties = [
                                        (View.Text, "TheQuickBrown"),
                                        (Label.FontSize, 26.0),
                                    ],
                                },
                                new Label {
                                    Properties = [
                                        (View.Text, " "),
                                        (Label.FontSize, 26.0),
                                    ],
                                },
                                new Label {
                                    Properties = [
                                        (View.Text, "    "),
                                        (Label.FontSize, 26.0),
                                    ],
                                },
                            ]
                        },
                        new Row {
                            Controls = [
                                new Label {
                                    Properties = [
                                        (View.Text, "1 The Quick Brown"),
                                        (Label.FontSize, 40.0),
                                        (Label.FontName, "Times New Roman"),
                                    ],
                                },
                                new Label {
                                    Properties = [
                                        (View.Text, "2 Fox Jumps Over"),
                                        (Label.FontSize, 26.0),
                                    ],
                                },
                                new Label {
                                    Properties = [
                                        (View.Text, "3 The Lazy Dog"),
                                        (Label.FontSize, 26.0),
                                    ],
                                },
                                new Label {
                                    Properties = [
                                        (View.Text, "4 And a Cow"),
                                        (Label.FontSize, 26.0),
                                    ],
                                },
                                new Label {
                                    Properties = [
                                        (View.Text, "5 And a Zebra"),
                                        (Label.FontSize, 16.0),
                                        (View.AlignVertical, VerticalAlignment.Center),
                                    ],
                                },
                            ]
                        },
                        new Row {
                            Controls = [
                                new Label {
                                    Properties = [
                                        (View.Text, "The Quick Brown"),
                                        (Label.FontSize, 40.0),
                                        (Label.FontName, "Times New Roman"),
                                    ],
                                },
                                new Label {
                                    Properties = [
                                        (View.Text, "Fox Jumps Over"),
                                        (Label.FontSize, 26.0),
                                    ],
                                },
                                new Label {
                                    Properties = [
                                        (View.Text, "The Lazy Dog"),
                                        (Label.FontSize, 26.0),
                                    ],
                                },
                                new Label {
                                    Properties = [
                                        (View.Text, "And a Cow"),
                                        (Label.FontSize, 32.0),
                                    ],
                                },
                                new Label {
                                    Properties = [
                                        (View.Text, "Center"),
                                        (Label.FontSize, 16.0),
                                        (View.AlignVertical, VerticalAlignment.Center),
                                    ],
                                },
                                new Label {
                                    Properties = [
                                        (View.Text, "Top"),
                                        (Label.FontSize, 16.0),
                                        (View.AlignVertical, VerticalAlignment.Top),
                                    ],
                                },
                                new Label {
                                    Properties = [
                                        (View.Text, "Bottom"),
                                        (Label.FontSize, 16.0),
                                        (View.AlignVertical, VerticalAlignment.Bottom),
                                    ],
                                },
                                new Label {
                                    Properties = [
                                        (View.Text, "Stretch"),
                                        (Label.FontSize, 16.0),
                                        (View.AlignVertical, VerticalAlignment.Stretch),
                                    ],
                                },
                            ],
                            Properties = [(Row.Wrap, true)],
                        },
                        new Row {
                            Controls = [
                                new Label { Properties = [(View.Text, "Done"),(Row.Wrap, true)], },
                            ]
                        },
                        new Label { Properties = [(View.Text, "Done")],}
                    ]
                },
            ]
        };

    }

}
