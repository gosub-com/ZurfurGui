namespace ZurfurGui.Controls
{
    public class TestView
    {
        public static Controllable Test()
        {
            var control = new Window
            {
                Text = "Test",
                Controls = [
                    new Column {
                        Controls = [
                            new Button { Text = "T 1" },
                            new Button { Text = "Test 2" },
                            new Row {
                                Controls = [
                                    new Button { Text = "Row A"},
                                    new Button { Text = "Row B"},
                                    new Button { Text = "Row C"},
                                ]
                            },
                            new Button { Text = "Test 3", HorizontalAlignment = HorizontalAlignment.Right },
                            new Button { Text = "Test 4" },
                        ]
                    },
                ]
            };


            control.PopulateView();

            return control;
        }
    }
}
