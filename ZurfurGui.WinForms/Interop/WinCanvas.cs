using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Platform;

namespace ZurfurGui.WinForms.Interop;

internal class WinCanvas : OsCanvas
{
    PictureBox _pictureBox;
    WinContext _context;

    public WinCanvas(PictureBox pictureBox)
    {
        _pictureBox = pictureBox;
        _context = new WinContext(pictureBox.CreateGraphics());
    }

    public OsContext Context => _context;

    public Size DeviceSize 
    {
        get => new Size(_pictureBox.Width, _pictureBox.Height); 
        set { }
    }

    public Rect GetBoundingClientRect()
    {
        return new Rect(new Point(_pictureBox.Left, _pictureBox.Top), DeviceSize);
    }

    public Size ClientSize
    {
        get => new Size(_pictureBox.Width, _pictureBox.Height);
    }

}
