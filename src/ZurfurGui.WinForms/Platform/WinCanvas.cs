using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using ZurfurGui.Base;
using ZurfurGui.Platform;

using Size = ZurfurGui.Base.Size;
using Point = ZurfurGui.Base.Point;
using ZurfurGui.Input;

namespace ZurfurGui.WinForms.Interop;

internal class WinCanvas : OsCanvas
{
    PictureBox _pictureBox;
    WinContext _context;
    Action<OsPointerEvent>? _pointerInput;
    readonly PointerWindow _pointerWindow;
    bool _nativeContactActive;
    public OsContext Context => _context;


    public WinCanvas(PictureBox pictureBox)
    {
        _pictureBox = pictureBox;
        _context = new WinContext(pictureBox.CreateGraphics());
        _pointerWindow = new PointerWindow(pictureBox, OnNativePointerMessage);

        pictureBox.MouseMove += (s, e) =>
        {
            if (!_nativeContactActive)
                _pointerInput?.Invoke(CreatePointerEvent("pointermove", e));
        };
        pictureBox.MouseDown += (s, e) =>
        {
            if (!_nativeContactActive)
                _pointerInput?.Invoke(CreatePointerEvent("pointerdown", e));
        };
        pictureBox.MouseUp += (s, e) =>
        {
            if (!_nativeContactActive)
                _pointerInput?.Invoke(CreatePointerEvent("pointerup", e, true));
        };
        pictureBox.MouseLeave += (s, e) => _pointerInput?.Invoke(new OsPointerEvent("pointerleave", new(-1, -1))
        {
            Buttons = MapButtons(Control.MouseButtons),
            Modifiers = MapModifiers(Control.ModifierKeys),
            Device = PointerDeviceKind.Mouse
        });
    }

    void OnNativePointerMessage(OsPointerEvent pointerEvent, bool isTerminal)
    {
        _nativeContactActive = !isTerminal;
        _pointerInput?.Invoke(pointerEvent);
        if (isTerminal)
            _nativeContactActive = false;
    }

    static OsPointerEvent CreatePointerEvent(string type, MouseEventArgs e, bool isUp = false)
    {
        var changedButton = MapButton(e.Button);
        var buttons = MapButtons(Control.MouseButtons);
        if (isUp)
            buttons &= ~changedButton;
        else
            buttons |= changedButton;

        return new OsPointerEvent(type, new(e.X, e.Y))
        {
            Buttons = buttons,
            ChangedButton = changedButton,
            Modifiers = MapModifiers(Control.ModifierKeys),
            Device = PointerDeviceKind.Mouse
        };
    }

    static PointerButtons MapButtons(MouseButtons value)
    {
        var buttons = PointerButtons.None;
        if ((value & MouseButtons.Left) != 0) buttons |= PointerButtons.Left;
        if ((value & MouseButtons.Right) != 0) buttons |= PointerButtons.Right;
        if ((value & MouseButtons.Middle) != 0) buttons |= PointerButtons.Middle;
        if ((value & MouseButtons.XButton1) != 0) buttons |= PointerButtons.Back;
        if ((value & MouseButtons.XButton2) != 0) buttons |= PointerButtons.Forward;
        return buttons;
    }

    static PointerButtons MapButton(MouseButtons value) => value switch
    {
        MouseButtons.Left => PointerButtons.Left,
        MouseButtons.Right => PointerButtons.Right,
        MouseButtons.Middle => PointerButtons.Middle,
        MouseButtons.XButton1 => PointerButtons.Back,
        MouseButtons.XButton2 => PointerButtons.Forward,
        _ => PointerButtons.None
    };

    static PointerModifiers MapModifiers(Keys value)
    {
        var modifiers = PointerModifiers.None;
        if ((value & Keys.Shift) != 0) modifiers |= PointerModifiers.Shift;
        if ((value & Keys.Control) != 0) modifiers |= PointerModifiers.Control;
        if ((value & Keys.Alt) != 0) modifiers |= PointerModifiers.Alt;
        if ((value & (Keys.LWin | Keys.RWin)) != 0) modifiers |= PointerModifiers.Meta;
        return modifiers;
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

    public Action<OsPointerEvent>? PointerInput 
    {
        get => _pointerInput;
        set { _pointerInput = value; }
    }

    // Intercepts native pointer messages because WinForms mouse events do not preserve touch or pen identity.
    // WinCanvas still uses mouse events as a fallback for ordinary mouse input.
    sealed class PointerWindow : NativeWindow
    {
        const int WM_POINTERDOWN = 0x0246;
        const int WM_POINTERUPDATE = 0x0245;
        const int WM_POINTERUP = 0x0247;
        const int WM_POINTERCANCEL = 0x0248;

        readonly PictureBox _pictureBox;
        readonly Action<OsPointerEvent, bool> _pointerInput;

        public PointerWindow(PictureBox pictureBox, Action<OsPointerEvent, bool> pointerInput)
        {
            _pictureBox = pictureBox;
            _pointerInput = pointerInput;
            AssignHandle(pictureBox.Handle);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg is WM_POINTERDOWN or WM_POINTERUPDATE or WM_POINTERUP or WM_POINTERCANCEL)
            {
                var pointerId = (uint)(m.WParam.ToInt64() & 0xffff);
                if (TryCreatePointerEvent(m.Msg, pointerId, out var pointerEvent, out var isTerminal))
                    _pointerInput(pointerEvent, isTerminal);
            }

            base.WndProc(ref m);
        }

        bool TryCreatePointerEvent(int message, uint pointerId, out OsPointerEvent pointerEvent, out bool isTerminal)
        {
            pointerEvent = null!;
            isTerminal = message is WM_POINTERUP or WM_POINTERCANCEL;
            if (!GetPointerType(pointerId, out var pointerType) || !GetPointerInfo(pointerId, out var info))
                return false;

            var device = pointerType switch
            {
                2 => PointerDeviceKind.Touch,
                3 => PointerDeviceKind.Pen,
                _ => PointerDeviceKind.Unknown
            };
            if (device == PointerDeviceKind.Unknown)
                return false;

            var clientPoint = _pictureBox.PointToClient(new System.Drawing.Point(info.Location.X, info.Location.Y));
            var kind = message switch
            {
                WM_POINTERDOWN => "pointerdown",
                WM_POINTERUP => "pointerup",
                WM_POINTERCANCEL => "pointercancel",
                _ => "pointermove"
            };

            pointerEvent = new OsPointerEvent(kind, new Point(clientPoint.X, clientPoint.Y))
            {
                Device = device,
                ContactId = (int)pointerId,
                Buttons = MapPointerButtons(info.Flags),
                ChangedButton = PointerButtons.None,
                Modifiers = MapModifiers(Control.ModifierKeys)
            };
            return true;
        }

        static PointerButtons MapPointerButtons(uint flags)
        {
            var buttons = PointerButtons.None;
            if ((flags & 0x10) != 0) buttons |= PointerButtons.Left;
            if ((flags & 0x20) != 0) buttons |= PointerButtons.Right;
            if ((flags & 0x40) != 0) buttons |= PointerButtons.Middle;
            if ((flags & 0x80) != 0) buttons |= PointerButtons.Back;
            if ((flags & 0x100) != 0) buttons |= PointerButtons.Forward;
            return buttons;
        }

        [DllImport("user32.dll")]
        static extern bool GetPointerType(uint pointerId, out uint pointerType);

        [DllImport("user32.dll")]
        static extern bool GetPointerInfo(uint pointerId, out PointerInfo pointerInfo);

        [StructLayout(LayoutKind.Sequential)]
        struct PointerInfo
        {
            public uint Type;
            public uint Id;
            public uint FrameId;
            public uint Flags;
            public IntPtr SourceDevice;
            public IntPtr Target;
            public NativePoint Location;
            public NativePoint HimetricLocation;
            public uint Time;
            public uint HistoryCount;
            public uint InputData;
            public uint KeyStates;
            public ulong PerformanceCount;
            public uint ButtonChangeType;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct NativePoint
        {
            public int X;
            public int Y;
        }
    }
}
