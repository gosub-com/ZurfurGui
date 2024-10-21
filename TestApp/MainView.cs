namespace TestApp;

using ZurfurGui;
using ZurfurGui.Controls;


public class MainView
{
    public static Controllable CreateView()
    {
        var w1 = new Window {
            Name = "main",
            Text = "Test1",
            Margin = new(115, 175, 0, 0),
            Size = new(400, 500),
            AlignHorizontal = HorizontalAlignment.Left,
            AlignVertical = VerticalAlignment.Top,
            Controls = [
                new Column {
                    Controls = [
                        new Button { Name="", Text = "Button1", AlignHorizontal = HorizontalAlignment.Center },
                        new Button { Name="", Text = "Test 3", AlignHorizontal = HorizontalAlignment.Center },
                        new Button { Name="", Text = "Test 3", AlignHorizontal = HorizontalAlignment.Right },
                        new Button { Name="", Text = "Test 3", AlignHorizontal = HorizontalAlignment.Left },
                        new Row {
                            Controls = [
                                new Button { Name="", Text = "Button A"},
                                new Button { Name="", Text = "Button B"},
                                new Button { Name="", Text = "Button C"},
                            ]
                        },
                        new Row {
                            Controls = [
                                new Label { Name="", Text = "The"},
                                new Label { Name="", Text = "Quick Brown Fox Jumps Over"},
                                new Label { Name="", Text = "The lazy dog"},
                            ]
                        },
                        new Row {
                            Controls = [
                                new Label { Name="", Text = "The", FontSize = 26},
                                new Label { Name="", Text = "Quick", FontSize = 26},
                                new Label { Name="", Text = "Brown", FontSize = 26},
                            ]
                        },
                        new Row {
                            Controls = [
                                new Label { Name="", Text = "The Quick Brown", FontSize = 26, Margin = new Thickness(-30, 0, 0, 0)},
                                new Label { Name="", Text = "Fox &Jumps", FontSize = 26},
                                new Label { Name="", Text = "Over the lazy dog", FontSize = 26},
                            ]
                        },
                        new Row {
                            Controls = [
                                new Label { Name="", Text = "Sans Topj", FontSize = 26, FontName="sans-serif"},
                                new Label { Name="", Text = "Arial Topj", FontSize = 26, FontName="Arial"},
                                new Label { Name="", Text = "﴿█j A﴿│|", FontSize = 26, FontName="Arial"},
                            ]
                        },
                        new Row {
                            Controls = [
                                new Label { Name="", Text = "TheQuickBrown", FontSize = 26},
                                new Label { Name="", Text = " ", FontSize = 26},
                                new Label { Name="", Text = "    ", FontSize = 26},
                            ]
                        },
                        new Row {
                            Controls = [
                                new Label { Name="", Text = "1 The Quick Brown", FontSize = 40, FontName = "Times New Roman"},
                                new Label { Name="", Text = "2 Fox Jumps Over", FontSize = 26},
                                new Label { Name="", Text = "3 The Lazy Dog", FontSize = 26},
                                new Label { Name="", Text = "4 And a Cow", FontSize = 26},
                                new Label { Name="", AlignVertical = VerticalAlignment.Center, Text = "5 And a Zebra", FontSize = 16},
                            ]
                        },
                        new Row {
                            Wrap = true,
                            Controls = [
                                new Label { Name="", Text = "The Quick Brown", FontSize = 40, FontName = "Times New Roman"},
                                new Label { Name="", Text = "Fox Jumps Over", FontSize = 26},
                                new Label { Name="", Text = "The Lazy Dog", FontSize = 26},
                                new Label { Name="", Text = "And a Cow", FontSize = 26},
                                new Label { Name="", AlignVertical = VerticalAlignment.Center, Text = "And a Zebra", FontSize = 16},
                            ]
                        },
                        new Row {
                            Wrap = true,
                            Controls = [
                                new Label { Name="", Text = "Done", },
                            ]
                        },
                        new Label { Text = "Done"}
                    ]
                },
            ]
        };


        var w2 = new Window
        {
            Margin = new(600, 175, 0, 0),
            Size = new(300, 150),
            AlignHorizontal = HorizontalAlignment.Left,
            AlignVertical = VerticalAlignment.Top,

            Controls = [
                new Button { Text = "This Button Is a test", 
                    AlignVertical = VerticalAlignment.Top,
                    AlignHorizontal = HorizontalAlignment.Left,
                    Size = new(100,100),
                    Margin = new Thickness(250, 100, 0, 0),
                },

                new Window {
                    AlignHorizontal = HorizontalAlignment.Left,
                    AlignVertical = VerticalAlignment.Top,
                    Margin = new Thickness(30, 30, 0, 0),
                    Size = new(50,50),
                },
                new Label { Name="", Text = "TC", AlignHorizontal = HorizontalAlignment.Center, AlignVertical = VerticalAlignment.Top },
                new Label { Name="", Text = "TS", AlignHorizontal = HorizontalAlignment.Stretch, AlignVertical = VerticalAlignment.Top },
                new Label { Name="", Text = "TR", AlignHorizontal = HorizontalAlignment.Right, AlignVertical = VerticalAlignment.Top },
                new Label { Name="", Text = "TL", AlignHorizontal = HorizontalAlignment.Left, AlignVertical = VerticalAlignment.Top },
                new Label { Name="", Text = "Middle", AlignHorizontal = HorizontalAlignment.Center, AlignVertical = VerticalAlignment.Center },
                new Label { Name="", Text = "CS", AlignHorizontal = HorizontalAlignment.Stretch, AlignVertical = VerticalAlignment.Center },
                new Label { Name="", Text = "CR", AlignHorizontal = HorizontalAlignment.Right, AlignVertical = VerticalAlignment.Center },
                new Label { Name="", Text = "CL", AlignHorizontal = HorizontalAlignment.Left, AlignVertical = VerticalAlignment.Center },
                new Label { Name="", Text = "BC", AlignHorizontal = HorizontalAlignment.Center, AlignVertical = VerticalAlignment.Bottom },
                new Label { Name="", Text = "BS", AlignHorizontal = HorizontalAlignment.Stretch, AlignVertical = VerticalAlignment.Bottom },
                new Label { Name="", Text = "BR", AlignHorizontal = HorizontalAlignment.Right, AlignVertical = VerticalAlignment.Bottom },
                new Label { Name="", Text = "BL", AlignHorizontal = HorizontalAlignment.Left, AlignVertical = VerticalAlignment.Bottom },
            ]

        };

        var mainWindow = new Canvas(){
            Margin = new(10, 10, 0, 0),
            Size = new(1000, 700),
            AlignHorizontal = HorizontalAlignment.Left,
            AlignVertical = VerticalAlignment.Top,
            Controls = [w1, w2]
        };




        return mainWindow;
    }
}
