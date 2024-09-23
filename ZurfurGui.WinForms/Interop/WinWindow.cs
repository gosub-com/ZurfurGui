﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Platform;

namespace ZurfurGui.WinForms.Interop;

internal class WinWindow(Form _form, PictureBox _picture) : OsWindow
{

    public double DevicePixelRatio => 1;

    public Size InnerSize => new Size(_picture.Width, _picture.Height);

    public Size OuterSize => new Size(_form.Width, _form.Height);

    public Size? ScreenSize => new Size(
        Screen.PrimaryScreen?.Bounds.Width??0, Screen.PrimaryScreen?.Bounds.Height??0);

    public Size? AvailScreenSize => new Size(_picture.Width, _picture.Height);
}
