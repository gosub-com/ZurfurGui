using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Platform;

namespace ZurfurGui.WinForms.Interop;

internal class WinGlobal : OsGlobal
{

    public WinGlobal(Form form, PictureBox canvas)
    {
        PrimaryWindow = new WinWindow(form, canvas);
        PrimaryCanvas = new WinCanvas(canvas);
    }

    public OsWindow PrimaryWindow { get; private set; }

    public OsCanvas PrimaryCanvas { get; private set; }
}
