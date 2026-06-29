using System;
using System.Collections.Generic;
using System.Text;
using ZurfurGui.Base;
using ZurfurGui.Input;

namespace ZurfurGui.Platform;

public interface OsCanvas
{
    public OsContext Context { get; }

    /// <summary>
    /// Size of canvas on screen in physical device pixels.  Not all browsers support this, so it can be null.
    /// A good fall back is ClientSize*DevicePixelRatio. See https://web.dev/articles/device-pixel-content-box
    /// </summary>
    public Size? DevicePixelSize { get => null; }


    /// <summary>
    /// Size of canvas pixel buffer, which ideally should match the number of pixels it occupies on the screen.
    /// </summary>
    public Size DeviceSize { get; set; }

    /// <summary>
    /// Size of canvas in CSS pixels
    /// </summary>
    public Size StyleSize { get; set; }

    /// <summary>
    /// True if the canvas has the focus
    /// </summary>
    public bool HasFocus { get; }

    /// <summary>
    /// Observe the pointer input
    /// </summary>
    public Action<PointerEvent>? PointerInput { get; set; }
}
