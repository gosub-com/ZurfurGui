using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Controls;

namespace ZurfurGui;

public static class Initialize
{
    /// <summary>
    /// Initialize the library with the built in controls, etc.
    /// </summary>
    public static void Init()
    {
        ControlRegistry.Add("ZGui.Button", () => new Button());
        ControlRegistry.Add("ZGui.Canvas", () => new Canvas());
        ControlRegistry.Add("ZGui.Column", () => new Column());
        ControlRegistry.Add("ZGui.Label", () => new Label());
        ControlRegistry.Add("ZGui.Row", () => new Row());
        ControlRegistry.Add("ZGui.Window", () => new Window());
    }
}
