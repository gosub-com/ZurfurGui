namespace TestApp;

using ZurfurGui;
using ZurfurGui.Components;
using ZurfurGui.Controls;
using ZurfurGui.Property;
using ZurfurGui.Windows;

public static class ZurfurMain
{
    public static void MainApp(AppWindow app)
    {
        ZurfurControls.Register();
        app.SetMainappWindow(new FormQbfWinTest());

        var win0 = Loader.LoadJson("""
        {
          "Controller": "Window",
          "Offset": { "X": 10, "Y": 10 },
          "AlignHorizontal": "Left",
          "AlignVertical": "Top",
          "Content": [
            {
              "Controller": "DebugWindow"
            }
          ]
        }
        """);
        app.ShowWindow((Window)win0);


        var win1 = Loader.LoadJson("""
        {
          "Controller": "Window",
          "SizeRequest": { "Width": 400, "Height": 500 },
          "Offset": { "X": 95, "Y": 175 },
          "AlignHorizontal": "Left",
          "AlignVertical": "Top",
          "Content": [
            {
              "Controller": "TestApp.FormQbfWinTest"
            }
          ]
        }
        """);
        app.ShowWindow((Window)win1);


        var win2 = Loader.LoadJson("""
        {
          "Controller": "Window",
          "Margin": { "Left": 600, "Top": 175, "Right": 0, "Bottom": 0 },
          "SizeRequest": { "Width": 200, "Height": 200 },
          "AlignHorizontal": "Left",
          "AlignVertical": "Top",
          "Content": [
            {
              "Controller": "TestApp.FormSmallWinTest",
              "Name": "smallWinTest"
            }
          ]
        }
        """);
        app.ShowWindow((Window)win2);
        var smallWinTest = (FormSmallWinTest)win2.View.FindByName("smallWinTest").Controller;
        smallWinTest.button1.View.SetProperty(Zui.Text, new(["*BUTTON*"]));

        var win3 = Loader.LoadJson("""
        {
          "Controller": "Window",
          "SizeRequest": { "Width": 200, "Height": 200 },
          "AlignHorizontal": "Right",
          "AlignVertical": "Bottom",
          "Margin": { "Left": 10, "Top": 10, "Right": 10, "Bottom": 10 },
          "Content": [
            {
              "Controller": "TestApp.FormSmallWinTest"
            }
          ]
        }
        """);
        app.ShowWindow((Window)win3);

        var win4 = Loader.LoadJson("""
        {
          "Controller": "Window",
          "AlignHorizontal": "Left",
          "AlignVertical": "Bottom",
          "Margin": { "Left": 10, "Top": 10, "Right": 10, "Bottom": 10 },
          "Content": [
            {
              "Controller": "TestApp.FormSearch"
            }
          ]
        }
        """);
        app.ShowWindow((Window)win4);
        ((Window)win4).IsWindowWrappingVisible = false;


        var win5 = Loader.LoadJson("""
        {
          "Controller": "Window",
          "AlignHorizontal": "Right",
          "AlignVertical": "Top",
          "Margin": { "Left": 10, "Top": 10, "Right": 10, "Bottom": 10 },
          "Content": [
            {
              "Controller": "TestApp.FormMultiForm"
            }
          ]
        }
        """);
        app.ShowWindow((Window)win5);

    }

}
