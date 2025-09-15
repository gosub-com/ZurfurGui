using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Base;
using ZurfurGui.Platform;

using Size = ZurfurGui.Base.Size;
using Point = ZurfurGui.Base.Point;

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

        pictureBox.MouseMove += (s, e) => _pointerInput?.Invoke(new PointerEvent("pointermove", new(e.X, e.Y))); ;
        pictureBox.MouseDown += (s, e) => _pointerInput?.Invoke(new PointerEvent("pointerdown", new(e.X, e.Y)));
        pictureBox.MouseUp += (s, e) => _pointerInput?.Invoke(new PointerEvent("pointerup", new(e.X, e.Y))); ;
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
