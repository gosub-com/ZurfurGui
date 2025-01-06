namespace TestApp;

using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using ZurfurGui;
using ZurfurGui.Components;
using ZurfurGui.Controls;

public class Main
{
    public static void MainApp(AppWindow app)
    {
        ControlRegistry.Add(() => { return new FormSearch(); });

        
        // An alternate way of creating the window below
        var win1v = new Window();
        win1v.SetContent(new QbfWinTest().View);
        var p1 = win1v.View.Properties;
        p1.Set(Zui.Size, new Size(400, 500));
        p1.Set(Zui.Margin, new Thickness(95, 175, 0, 0));
        p1.Set(Zui.AlignHorizontal, AlignHorizontal.Left);
        p1.Set(Zui.AlignVertical, AlignVertical.Top);
        app.AddWindow(win1v);
        
        /*
        Properties win1 = [
            (Zui.Controller, "Zui.Window"),
            (Zui.Size, new Size(400, 500)),
            (Zui.Margin, new Thickness(95, 175, 0, 0)),
            (Zui.AlignHorizontal, AlignHorizontal.Left),
            (Zui.AlignVertical, AlignVertical.Top),
            (Zui.Content, (Properties[])[[(Zui.Controller, "Zui.QbfWinTest")]])
        ];
        app.AddWindow((Window)Helper.BuildView(win1).Control);
        */

        Properties win2 = [
            (Zui.Controller, "Zui.Window"),
            (Zui.Margin, new Thickness(600, 175, 0, 0)),
            (Zui.Size, new Size(200, 200)),
            (Zui.AlignHorizontal, AlignHorizontal.Left),
            (Zui.AlignVertical, AlignVertical.Top),
            (Zui.Content, (Properties[])[[(Zui.Controller, "Zui.SmallWinTest")]])
        ];
        app.AddWindow((Window)Helper.BuildView(win2).Control);

        Properties win3 = [
            (Zui.Controller, "Zui.Window"),
            (Zui.Size, new Size(200, 200)),
            (Zui.AlignHorizontal, AlignHorizontal.Right),
            (Zui.AlignVertical, AlignVertical.Bottom),
            (Zui.Margin, new Thickness(10)),
            (Zui.Content, (Properties[])[[(Zui.Controller, "Zui.SmallWinTest")]])
        ];
        app.AddWindow((Window)Helper.BuildView(win3).Control);

        Properties win4Props = [
            (Zui.Controller, "Zui.Window"),
            (Zui.AlignHorizontal, AlignHorizontal.Left),
            (Zui.AlignVertical, AlignVertical.Bottom),
            (Zui.Margin, new Thickness(10)),
            (Zui.Content, (Properties[])[[(Zui.Controller, "TestApp.FormSearch")]])
        ];
        var win4 = (Window)Helper.BuildView(win4Props).Control;
        app.AddWindow(win4);
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
        app.AddWindow(win5);
        win5.IsWindowWrappingVisible = false;

    }

}
