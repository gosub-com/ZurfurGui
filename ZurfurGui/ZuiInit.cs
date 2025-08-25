using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Controls;

namespace ZurfurGui;

public static class ZuiInit
{
    /// <summary>
    /// Initialize the library with the built in controls, etc.
    /// </summary>
    public static AppWindow Init(Action<AppWindow> mainAppEntry)
    {
        ControlRegistry.Add(() => new Panel());
        ControlRegistry.Add(() => new Button());
        ControlRegistry.Add(() => new Column());
        ControlRegistry.Add(() => new Label());
        ControlRegistry.Add(() => new Row());
        ControlRegistry.Add(() => new Border());
        ControlRegistry.Add(() => new TextBox());
        ControlRegistry.Add(() => new DockPanel());
        ControlRegistry.Add(() => new Window());
        ControlRegistry.Add(() => new AppWindow());

        var appWindow = new AppWindow();
        mainAppEntry(appWindow);
        return appWindow;
    }
}
