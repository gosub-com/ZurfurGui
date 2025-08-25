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

        Properties win1 = [
            (Zui.Controller, "Zui.Window"),
            (Zui.Size, new Size(400, 500)),
            (Zui.Margin, new Thickness(95, 175, 0, 0)),
            (Zui.AlignHorizontal, AlignHorizontal.Left),
            (Zui.AlignVertical, AlignVertical.Top),
            (Zui.Content, (Properties[])[[(Zui.Controller, "TestApp.FormQbfWinTest")]])
        ];
        app.ShowWindow((Window)Helper.BuildView(win1).Control);




        Properties win2 = [
            (Zui.Controller, "Zui.Window"),
            (Zui.Margin, new Thickness(600, 175, 0, 0)),
            (Zui.Size, new Size(200, 200)),
            (Zui.AlignHorizontal, AlignHorizontal.Left),
            (Zui.AlignVertical, AlignVertical.Top),
            (Zui.Content, (Properties[])[[(Zui.Controller, "TestApp.FormSmallWinTest")]])
        ];
        app.ShowWindow((Window)Helper.BuildView(win2).Control);

        Properties win3 = [
            (Zui.Controller, "Zui.Window"),
            (Zui.Size, new Size(200, 200)),
            (Zui.AlignHorizontal, AlignHorizontal.Right),
            (Zui.AlignVertical, AlignVertical.Bottom),
            (Zui.Margin, new Thickness(10)),
            (Zui.Content, (Properties[])[[(Zui.Controller, "TestApp.FormSmallWinTest")]])
        ];
        app.ShowWindow((Window)Helper.BuildView(win3).Control);

        Properties win4Props = [
            (Zui.Controller, "Zui.Window"),
            (Zui.AlignHorizontal, AlignHorizontal.Left),
            (Zui.AlignVertical, AlignVertical.Bottom),
            (Zui.Margin, new Thickness(10)),
            (Zui.Content, (Properties[])[[(Zui.Controller, "TestApp.FormSearch")]])
        ];
        var win4 = (Window)Helper.BuildView(win4Props).Control;
        app.ShowWindow(win4);
        win4.IsWindowWrappingVisible = false;

        Properties win5Props = [
            (Zui.Controller, "Zui.Window"),
            (Zui.AlignHorizontal, AlignHorizontal.Right),
            (Zui.AlignVertical, AlignVertical.Top),
            (Zui.Margin, new Thickness(10)),
            (Zui.Background, Colors.Red),
            (Zui.BorderColor, Colors.Yellow),
            (Zui.BorderWidth, 2.0),
            (Zui.Padding, new Thickness(50)),
            (Zui.BorderRadius, 10.0),
            (Zui.Content, (Properties[])[[(Zui.Controller, "TestApp.FormSearch")]])
        ];
        var win5 = (Window)Helper.BuildView(win5Props).Control;
        app.ShowWindow(win5);
        win5.IsWindowWrappingVisible = false;

    }



}
