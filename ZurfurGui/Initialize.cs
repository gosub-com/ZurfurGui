﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Components;
using ZurfurGui.Controls;

namespace ZurfurGui;

public static class Initialize
{
    /// <summary>
    /// Initialize the library with the built in controls, etc.
    /// </summary>
    public static void Init()
    {
        ControlRegistry.Add(() => new BackgroundTest());
        ControlRegistry.Add(() => new Button());
        ControlRegistry.Add(() => new Panel());
        ControlRegistry.Add(() => new Column());
        ControlRegistry.Add(() => new Label());
        ControlRegistry.Add(() => new Row());
        ControlRegistry.Add(() => new Window());

        ControlRegistry.Add(() => new SmallWinTest());
        ControlRegistry.Add(() => new QbfWinTest());
    }
}
