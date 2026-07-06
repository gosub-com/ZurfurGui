using System;
using System.Collections.Generic;
using System.Text;
using ZurfurGui.Base;

namespace ZurfurGui.Platform;

/// <summary>
/// Platform independent window.  All platofrms must implement this interface.
/// </summary>
public interface OsWindow
{
    /// <summary>
    /// Best guess as to how big a pixel should be on our device
    /// </summary>
    public double DevicePixelRatio { get; }

    /// <summary>
    /// Includes scroll bar
    /// </summary>
    public Size InnerSize { get; }

    /// <summary>
    /// Excludes scroll bar
    /// </summary>
    public Size OuterSize { get; }

    /// <summary>
    /// Device screen size
    /// </summary>
    public Size? ScreenSize { get; }

    /// <summary>
    /// Available device screen size
    /// </summary>
    public Size? AvailScreenSize { get; }
}
