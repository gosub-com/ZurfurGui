namespace TestApp;

using ZurfurGui;
using ZurfurGui.Controls;


public class MainView
{
    public static IEnumerable<Controllable> CreateView()
    {
        var smallWin = MakeSmallWinTest();

        var smallWinBR = MakeSmallWinTest();
        smallWinBR.View.Control?.Properties.Set(View.MarginPi, new(10));
        smallWinBR.View.Control?.Properties.Set(View.AlignHorizontalPi, HorizontalAlignment.Right);
        smallWinBR.View.Control?.Properties.Set(View.AlignVerticalPi, VerticalAlignment.Bottom);

        var canvas = new Canvas(){
            Properties = new([
                (View.MarginPi, new Thickness(10, 10, 10, 10)),
                (View.AlignHorizontalPi, HorizontalAlignment.Stretch),
                (View.AlignVerticalPi, VerticalAlignment.Stretch),
            ]),


        };

        return [MakeQbfWin(), smallWin, canvas, smallWinBR];
    }

    static Window MakeSmallWinTest()
    {
        return new Window
        {
            Properties = new([
                (View.MarginPi, new Thickness(600, 175, 0, 0)),
                (View.SizePi, new Size(150, 150)),
                (View.AlignHorizontalPi, HorizontalAlignment.Left),
                (View.AlignVerticalPi, VerticalAlignment.Top),
            ]),

            Controls = [
                new Button { Text = "This button is a test",
                    Properties = new([
                        (View.AlignVerticalPi, VerticalAlignment.Top),
                        (View.AlignHorizontalPi,  HorizontalAlignment.Left),
                        (View.SizePi, new Size(100, 100)),
                        (View.MarginPi, new Thickness(100, 100, 0, 0)),
                    ])
                },

                new Window {
                    Properties = new([
                        (View.AlignVerticalPi, VerticalAlignment.Top),
                        (View.AlignHorizontalPi, HorizontalAlignment.Left),
                        (View.SizePi, new Size(50, 50)),
                        (View.MarginPi, new Thickness(30, 30, 0, 0)),
                    ]),
                    Controls = [new Canvas()]
                },
                new Label {
                    Properties = new([
                        (View.Text, "TC"),
                        (View.AlignVerticalPi, VerticalAlignment.Top),
                        (View.AlignHorizontalPi, HorizontalAlignment.Center),
                    ])
                },
                new Label {
                    Properties = new([
                        (View.Text, "TS"),
                        (View.AlignVerticalPi, VerticalAlignment.Top),
                        (View.AlignHorizontalPi, HorizontalAlignment.Stretch),
                    ])
                },
                new Label {
                    Properties = new([
                        (View.Text, "TR"),
                        (View.AlignVerticalPi, VerticalAlignment.Top),
                        (View.AlignHorizontalPi, HorizontalAlignment.Right),
                    ])
                },
                new Label {
                    Properties = new([
                        (View.Text, "TL"),
                        (View.AlignVerticalPi, VerticalAlignment.Top),
                        (View.AlignHorizontalPi, HorizontalAlignment.Left),
                    ])
                },
                new Label {
                    Properties = new([
                        (View.Text, "Middle"),
                        (View.AlignVerticalPi, VerticalAlignment.Center),
                        (View.AlignHorizontalPi, HorizontalAlignment.Center),
                    ])
                },
                new Label {
                    Properties = new([
                        (View.Text, "CS"),
                        (View.AlignVerticalPi, VerticalAlignment.Center),
                        (View.AlignHorizontalPi, HorizontalAlignment.Stretch),
                    ])
                },
                new Label {
                    Properties = new([
                        (View.Text, "CR"),
                        (View.AlignVerticalPi, VerticalAlignment.Center),
                        (View.AlignHorizontalPi, HorizontalAlignment.Right),
                    ])
                },
                new Label {
                    Properties = new([
                        (View.Text, "CL"),
                        (View.AlignVerticalPi, VerticalAlignment.Center),
                        (View.AlignHorizontalPi, HorizontalAlignment.Left),
                    ])
                },
                new Label {
                    Properties = new([
                        (View.Text, "BC"),
                        (View.AlignVerticalPi, VerticalAlignment.Bottom),
                        (View.AlignHorizontalPi, HorizontalAlignment.Center),
                    ])
                },
                new Label {
                    Properties = new([
                        (View.Text, "BS"),
                        (View.AlignVerticalPi, VerticalAlignment.Bottom),
                        (View.AlignHorizontalPi, HorizontalAlignment.Stretch),
                    ])
                },
                new Label {
                    Properties = new([
                        (View.Text, "BR"),
                        (View.AlignVerticalPi, VerticalAlignment.Bottom),
                        (View.AlignHorizontalPi, HorizontalAlignment.Right),
                    ])
                },
                new Label {
                    Properties = new([
                        (View.Text, "BL"),
                        (View.AlignVerticalPi, VerticalAlignment.Bottom),
                        (View.AlignHorizontalPi, HorizontalAlignment.Left),
                    ])
                },
            ]
        };
    }

    static Window MakeQbfWin()
    {
        return new Window
        {
            Properties = new([
                (View.MarginPi, new Thickness(115, 175, 0, 0)),
                (View.SizePi, new Size(400, 500)),
                (View.AlignHorizontalPi, HorizontalAlignment.Left),
                (View.AlignVerticalPi, VerticalAlignment.Top),
            ]),


            Controls = [
                new Column {
                    Controls = [
                        new Button {
                            Properties = new([
                                (View.Text, "Button"),
                                (View.AlignHorizontalPi, HorizontalAlignment.Center),
                            ]),
                        },
                        new Button {
                            Properties = new([
                                (View.Text, "Button"),
                                (View.AlignHorizontalPi, HorizontalAlignment.Right),
                            ]),
                        },
                        new Button {
                            Properties = new([
                                (View.Text, "Button"),
                                (View.AlignHorizontalPi, HorizontalAlignment.Left),
                            ]),
                        },
                        new Row {
                            Controls = [
                                new Button { Properties = new([(View.Text, "Button A")])},
                                new Button { Properties = new([(View.Text, "Button B")])},
                                new Button { Properties = new([(View.Text, "Button C")])},
                            ]
                        },
                        new Row {
                            Controls = [
                                new Label { Properties = new([(View.Text, "The")])},
                                new Label { Properties = new([(View.Text, "Quick Brown Fox Jumps Over")])},
                                new Label { Properties = new([(View.Text, "The lazy dog")])},
                            ]
                        },
                        new Row {
                            Controls = [
                                new Label { Properties = new([(View.Text, "The"), (Label.FontSizePi, 16.0)])},
                                new Label { Properties = new([(View.Text, "Quick"), (Label.FontSizePi, 32.0)])},
                                new Label { Properties = new([(View.Text, "Brown"), (Label.FontSizePi, 26.0)])},
                            ]
                        },
                        new Row {
                            Controls = [
                                new Label {
                                    Properties = new([
                                        (View.Text, "The Quick Brown"),
                                        (Label.FontSizePi, 26.0),
                                        (View.MarginPi, new Thickness(-30, 0, 0, 0)),
                                    ]),
                                },
                                new Label {
                                    Properties = new([
                                        (View.Text, "Fox &Jumps"),
                                        (Label.FontSizePi, 26.0),
                                    ]),
                                },
                                new Label {
                                    Properties = new([
                                        (View.Text, "Over the lazy dog"),
                                        (Label.FontSizePi, 26.0),
                                    ]),
                                },
                            ]
                        },
                        new Row {
                            Controls = [
                                new Label {
                                    Properties = new([
                                        (View.Text, "Sans j"),
                                        (Label.FontSizePi, 26.0),
                                        (Label.FontNamePi, "sans-serif"),
                                    ]),
                                },
                                new Label {
                                    Properties = new([
                                        (View.Text, "Arial j"),
                                        (Label.FontSizePi, 26.0),
                                        (Label.FontNamePi, "Arial"),
                                    ]),
                                },
                                new Label {
                                    Properties = new([
                                        (View.Text, "﴿█j A﴿│|"),
                                        (Label.FontSizePi, 26.0),
                                        (Label.FontNamePi, "Arial"),
                                    ]),
                                },
                                new Label {
                                    Properties = new([
                                        (View.Text, "Serif j"),
                                        (Label.FontSizePi, 26.0),
                                        (Label.FontNamePi, "Times New Roman"),
                                    ]),
                                },
                            ]
                        },
                        new Row {
                            Controls = [
                                new Label {
                                    Properties = new([
                                        (View.Text, "TheQuickBrown"),
                                        (Label.FontSizePi, 26.0),
                                    ]),
                                },
                                new Label {
                                    Properties = new([
                                        (View.Text, " "),
                                        (Label.FontSizePi, 26.0),
                                    ]),
                                },
                                new Label {
                                    Properties = new([
                                        (View.Text, "    "),
                                        (Label.FontSizePi, 26.0),
                                    ]),
                                },
                            ]
                        },
                        new Row {
                            Controls = [
                                new Label {
                                    Properties = new([
                                        (View.Text, "1 The Quick Brown"),
                                        (Label.FontSizePi, 40.0),
                                        (Label.FontNamePi, "Times New Roman"),
                                    ]),
                                },
                                new Label {
                                    Properties = new([
                                        (View.Text, "2 Fox Jumps Over"),
                                        (Label.FontSizePi, 26.0),
                                    ]),
                                },
                                new Label {
                                    Properties = new([
                                        (View.Text, "3 The Lazy Dog"),
                                        (Label.FontSizePi, 26.0),
                                    ]),
                                },
                                new Label {
                                    Properties = new([
                                        (View.Text, "4 And a Cow"),
                                        (Label.FontSizePi, 26.0),
                                    ]),
                                },
                                new Label {
                                    Properties = new([
                                        (View.Text, "5 And a Zebra"),
                                        (Label.FontSizePi, 16.0),
                                        (View.AlignVerticalPi, VerticalAlignment.Center),
                                    ]),
                                },
                            ]
                        },
                        new Row {
                            Wrap = true,
                            Controls = [
                                new Label {
                                    Properties = new([
                                        (View.Text, "The Quick Brown"),
                                        (Label.FontSizePi, 40.0),
                                        (Label.FontNamePi, "Times New Roman"),
                                    ]),
                                },
                                new Label {
                                    Properties = new([
                                        (View.Text, "Fox Jumps Over"),
                                        (Label.FontSizePi, 26.0),
                                    ]),
                                },
                                new Label { Text = "The Lazy Dog", FontSize = 26,
                                    Properties = new([
                                        (View.Text, "The Lazy Dog"),
                                        (Label.FontSizePi, 26.0),
                                    ]),
                                },
                                new Label {
                                    Properties = new([
                                        (View.Text, "And a Cow"),
                                        (Label.FontSizePi, 32.0),
                                    ]),
                                },
                                new Label {
                                    Properties = new([
                                        (View.Text, "Center"),
                                        (Label.FontSizePi, 16.0),
                                        (View.AlignVerticalPi, VerticalAlignment.Center),
                                    ]),
                                },
                                new Label {
                                    Properties = new([
                                        (View.Text, "Top"),
                                        (Label.FontSizePi, 16.0),
                                        (View.AlignVerticalPi, VerticalAlignment.Top),
                                    ]),
                                },
                                new Label {
                                    Properties = new([
                                        (View.Text, "Bottom"),
                                        (Label.FontSizePi, 16.0),
                                        (View.AlignVerticalPi, VerticalAlignment.Bottom),
                                    ]),
                                },
                                new Label {
                                    Properties = new([
                                        (View.Text, "Stretch"),
                                        (Label.FontSizePi, 16.0),
                                        (View.AlignVerticalPi, VerticalAlignment.Stretch),
                                    ]),
                                },
                            ],
                            Properties = new([(Row.WrapPi, true)]),
                        },
                        new Row {
                            Wrap = true,
                            Controls = [
                                new Label { Properties = new([(View.Text, "Done")]), },
                            ]
                        },
                        new Label { Properties = new([(View.Text, "Done")]),}
                    ]
                },
            ]
        };

    }

}
