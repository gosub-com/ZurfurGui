using System;
using System.Collections.Generic;
using System.Text;
using ZurfurGui.Base;

namespace ZurfurGui.Platform;

/// <summary>
/// Even though the public facing draw API uses modern Pens and Brushes,
/// the underlying implementation still uses the canvas API.
/// </summary>
public interface OsContext
{
    public Color FillColor { get; set; }
    public Color StrokeColor { get; set; }
    public double LineWidth { get; set; }
    public string FontName { set; get; }
    public double FontSize { set; get; }


    public void FillRect(double x, double y, double width, double height, double radius);

    public void StrokeRect(double x, double y, double width, double height, double radius);

    /// <summary>
    /// Draw at the alphabetic base line
    /// </summary>
    public void FillText(string text, double x, double y);

    public double MeasureTextWidth(string text);

    public void StrokePolyLine(double[] points, int length);

    public void FillPolygon(double[] points, int length);

    public void Clip(double x, double y, double width, double height);

    public void UnClip();
}
