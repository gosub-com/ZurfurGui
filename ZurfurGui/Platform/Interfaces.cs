using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading.Tasks;

namespace ZurfurGui.Platform;



public interface OsWindow
{
    public OsCanvas PrimaryCanvas { get; }

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

}

public interface OsContext
{
    public Color FillColor { get; set; }
    public Color StrokeColor { get; set; }
    public double LineWidth { get; set; }
    public string FontName { set; get; }
    public double FontSize { set; get; }


    public void FillRect(double x, double y, double width, double height);

    public void StrokeRect(double x, double y, double width, double height);

    /// <summary>
    /// Draw at the alphabetic base line
    /// </summary>
    public void FillText(string text, double x, double y);

    public double MeasureTextWidth(string text);
    public void ClipRect(double x, double y, double width, double height);
}
