namespace TestApp;

using ZurfurGui;
using ZurfurGui.Controls;


public class MainView
{
    public static Controllable CreateView()
    {
        return new Window {
            Name = "main",
            Text = "Test1",
            SizeMin = new Size(0, 0),
            Controls = [
                new Column {
                    Controls = [
                        new Label { Name="", Text = "Button1", AlignHorizontal = HorizontalAlignment.Center },
                        new Label { Name="", Text = "Test 3", AlignHorizontal = HorizontalAlignment.Center },
                        new Label { Name="", Text = "Test 3", AlignHorizontal = HorizontalAlignment.Right },
                        new Label { Name="", Text = "Test 3", AlignHorizontal = HorizontalAlignment.Left },
                        new Row {
                            Controls = [
                                new Label { Name="", Text = "Button A"},
                                new Label { Name="", Text = "Button B"},
                                new Label { Name="", Text = "Button C"},
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
    }
}
