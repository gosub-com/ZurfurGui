﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Platform;

namespace ZurfurGui.Browser.Interop;

internal class BrowserWindow(JSObject _js) : OsWindow
{
    public double DevicePixelRatio => _js.GetPropertyAsDouble("devicePixelRatio");

    /// <summary>
    /// Includes scroll bar
    /// </summary>
    public Size InnerSize
        => new Size(_js.GetPropertyAsDouble("innerWidth"), _js.GetPropertyAsDouble("innerHeight"));

    /// <summary>
    /// Excludes scroll bar
    /// </summary>
    public Size OuterSize
        => new Size(_js.GetPropertyAsDouble("outerWidth"), _js.GetPropertyAsDouble("outerHeight"));

    /// <summary>
    /// Device screen size
    /// </summary>
    public Size? ScreenSize
    {
        get
        {
            var s = _js.GetPropertyAsJSObject("screen");
            if (s == null)
                return null;
            return new Size(s.GetPropertyAsDouble("width"), s.GetPropertyAsDouble("height"));
        }
    }

    /// <summary>
    /// Avalable device screen size
    /// </summary>
    public Size? AvailScreenSize
    {
        get
        {
            var s = _js.GetPropertyAsJSObject("screen");
            if (s == null)
                return null;
            return new Size(s.GetPropertyAsDouble("availWidth"), s.GetPropertyAsDouble("availHeight"));
        }
    }
}

