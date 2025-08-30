namespace TestApp;

using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using ZurfurGui;
using ZurfurGui.Components;
using ZurfurGui.Controls;
using System.Text.Json;
using System.Text.Json.Serialization;
public class ZurfurMain
{
    public static void MainApp(AppWindow app)
    {
        ControlRegistry.Add(() => new FormSearch());
        ControlRegistry.Add(() => new FormSmallWinTest());
        ControlRegistry.Add(() => new FormQbfWinTest());



        app.SetMainappWindow(new FormQbfWinTest());

        var win1 = Loader.LoadJson("""
        {
          "Controller": "Window",
          "Size": { "Width": 400, "Height": 500 },
          "Margin": { "Left": 95, "Top": 175, "Right": 0, "Bottom": 0 },
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
          "Size": { "Width": 200, "Height": 200 },
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
        var smallWinTest = (FormSmallWinTest)win2.View.FindByName("smallWinTest").Control;
        smallWinTest.button1.View.Properties.Set(Zui.Text, "*BUTTON*");

        var win3 = Loader.LoadJson("""
        {
          "Controller": "Window",
          "Size": { "Width": 200, "Height": 200 },
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

        var win5Unused = Loader.LoadJson("""
        {
          "Controller": "Window",
          "AlignHorizontal": "Right",
          "AlignVertical": "Top",
          "Margin": { "Left": 10, "Top": 10, "Right": 10, "Bottom": 10 },
          "Background": "Red",
          "BorderColor": "Yellow",
          "BorderWidth": 2.0,
          "Padding": { "Left": 50, "Top": 50, "Right": 50, "Bottom": 50 },
          "BorderRadius": 10.0,
          "Content": [
            {
              "Controller": "TestApp.FormSearch"
            }
          ]
        }
        """);
        var win5 = Loader.LoadJson("""
        {
          "Controller": "Window",
          "AlignHorizontal": "Right",
          "AlignVertical": "Top",
          "Margin": { "Left": 10, "Top": 10, "Right": 10, "Bottom": 10 },
          "Background": "Red",
          "BorderColor": "Yellow",
          "BorderWidth": 2.0,
          "Padding": { "Left": 50, "Top": 50, "Right": 50, "Bottom": 50 },
          "BorderRadius": 10.0,
          "Content": [
            {
              "Controller": "Column",
              "Content": [
                {
                  "Controller": "Border",
                  "Background": "Blue",
                  "Content": [
                    {
                      "Controller": "TestApp.FormSmallWinTest"
                    }
                  ]
                },
                {
                  "Controller": "Border",
                  "Background": "Purple",
                  "Content": [
                    {
                      "Controller": "TestApp.FormSearch"
                    }
                  ]
                }
              ]
            }
          ]
        }
        """);
        app.ShowWindow((Window)win5);
        ((Window)win5).IsWindowWrappingVisible = false;

    }

}
