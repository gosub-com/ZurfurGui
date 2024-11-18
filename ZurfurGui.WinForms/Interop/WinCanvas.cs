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
    Action<PointerEvent>? _pointerInput;
    public OsContext Context => _context;


    public WinCanvas(PictureBox pictureBox)
    {
        _pictureBox = pictureBox;
        _context = new WinContext(pictureBox.CreateGraphics());

        pictureBox.MouseMove += PictureBox_MouseMove;
    }

    private void PictureBox_MouseMove(object? sender, MouseEventArgs e)
    {
        _pointerInput?.Invoke(new PointerEvent("pointermove", new(e.X, e.Y)));
    }


    public Size DeviceSize 
    {
        get => new Size(_pictureBox.Width, _pictureBox.Height); 
        set => throw new NotImplementedException();
    }

    public Rect GetBoundingClientRect()
    {
        return new Rect(new Point(_pictureBox.Left, _pictureBox.Top), DeviceSize);
    }

    public Size StyleSize
    {
        get => new Size(_pictureBox.Width, _pictureBox.Height);
        set => throw new NotImplementedException();
    }

    public bool HasFocus => true;

    public Action<PointerEvent>? PointerInput 
    {
        get => _pointerInput;
        set { _pointerInput = value; }
    }
}
